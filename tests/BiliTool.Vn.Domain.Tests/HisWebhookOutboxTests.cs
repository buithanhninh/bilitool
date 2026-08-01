using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using BiliTool.Vn.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public class HisWebhookOutboxTests
{
    [Fact]
    public void Signature_UsesTimestampDotPayloadHmacSha256()
    {
        const string secret = "webhook-secret-at-least-32-characters";
        const long timestamp = 1785500000;
        const string payload = "{\"resultId\":\"calc_1\"}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = "v1=" + Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();

        Assert.Equal(expected, WebhookSignature.Create(secret, timestamp, payload));
    }

    [Fact]
    public async Task Provisioning_RequiresHttpsAndStoresProtectedSecret()
    {
        await using var db = CreateDbContext();
        var provider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        var service = new HisWebhookProvisioningService(db, provider);
        const string secret = "webhook-secret-at-least-32-characters";

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfigureAsync(
            "tenant-1", "client-1", new Uri("http://example.com/webhook"), secret, ["clinical.calculation.completed"]));

        await service.ConfigureAsync(
            "tenant-1", "client-1", new Uri("https://example.com/webhook"), secret, ["clinical.calculation.completed"]);

        var subscription = await db.HisWebhookSubscriptions.SingleAsync();
        Assert.DoesNotContain(secret, subscription.SecretProtected, StringComparison.Ordinal);
        var unprotected = provider.CreateProtector("BiliTool.Vn.HIS.WebhookSecret.v1").Unprotect(subscription.SecretProtected);
        Assert.Equal(secret, unprotected);
    }

    [Fact]
    public async Task ClinicalAudit_AtomicallyCreatesMatchingOutboxEvent()
    {
        await using var db = CreateDbContext();
        var subscription = new HisWebhookSubscription
        {
            TenantId = "tenant-1",
            ApiClientId = "client-1",
            EndpointUrl = "https://example.com/webhook",
            SecretProtected = "protected",
            EventTypes = "clinical.calculation.completed"
        };
        db.HisWebhookSubscriptions.Add(subscription);
        await db.SaveChangesAsync();
        var requestContext = new FakeRequestContext
        {
            TenantIdValue = "tenant-1",
            ApiClientIdValue = "client-1",
            CorrelationIdValue = "corr-1",
            ResultId = "calc_1"
        };
        var service = new ClinicalAuditService(db, NullLogger<ClinicalAuditService>.Instance, requestContext);

        await service.TryRecordCalculationAsync(
            new YeuCauTinhToanBilirubinDto { TuoiTheoGio = 48, TongBilirubin = 12, TuoiThaiTuan = 38 },
            new KetQuaTinhToanDto { TuoiGio = 48, TuoiThaiTuan = 38 },
            new BilirubinCalculationTrace { TuoiGio = 48, TuoiThaiTuan = 38 });

        Assert.Equal(1, await db.ClinicalAuditLogs.CountAsync());
        var outboxEvent = await db.HisOutboxEvents.SingleAsync();
        Assert.Equal("calc_1", outboxEvent.ResultId);
        using var payload = JsonDocument.Parse(outboxEvent.PayloadJson);
        Assert.Equal(outboxEvent.Id.ToString("N"), payload.RootElement.GetProperty("eventId").GetString());
        Assert.Equal("corr-1", payload.RootElement.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(8, 3600)]
    public void Backoff_IsExponentialAndCapped(int attempt, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), HisOutboxDeliveryService.CalculateBackoff(attempt));
    }

    [Fact]
    public async Task ResilienceGate_BulkheadRejectsSaturationWithoutBlockingPool()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Webhooks:Resilience:MaxConcurrency"] = "1",
            ["Webhooks:Resilience:BulkheadQueueTimeoutMs"] = "0"
        }).Build();
        using var gate = new HisWebhookResilienceGate(configuration);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var occupied = gate.ExecuteAsync(async () =>
        {
            started.SetResult();
            await release.Task;
            return new HisWebhookDeliveryResult(true, 204, null);
        }, CancellationToken.None);
        await started.Task;

        var rejected = await gate.ExecuteAsync(
            () => Task.FromResult(new HisWebhookDeliveryResult(true, 204, null)),
            CancellationToken.None);

        Assert.False(rejected.Succeeded);
        Assert.Contains("bulkhead", rejected.Error, StringComparison.OrdinalIgnoreCase);
        release.SetResult();
        Assert.True((await occupied).Succeeded);
    }

    [Fact]
    public async Task ResilienceGate_CircuitOpensAndRecoversThroughHalfOpenProbe()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Webhooks:Resilience:CircuitFailureThreshold"] = "2",
            ["Webhooks:Resilience:CircuitBreakSeconds"] = "1"
        }).Build();
        using var gate = new HisWebhookResilienceGate(configuration);
        var executions = 0;
        Task<HisWebhookDeliveryResult> Failure()
        {
            executions++;
            return Task.FromResult(new HisWebhookDeliveryResult(false, 503, "synthetic dependency failure"));
        }

        await gate.ExecuteAsync(Failure, CancellationToken.None);
        await gate.ExecuteAsync(Failure, CancellationToken.None);
        var open = await gate.ExecuteAsync(Failure, CancellationToken.None);

        Assert.Equal(2, executions);
        Assert.Contains("circuit breaker", open.Error, StringComparison.OrdinalIgnoreCase);
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        var recovered = await gate.ExecuteAsync(
            () => Task.FromResult(new HisWebhookDeliveryResult(true, 204, null)),
            CancellationToken.None);
        Assert.True(recovered.Succeeded);
    }

    [Fact]
    public async Task ReplayDeadLetter_ResetsDeliveryStateAndRecordsMetric()
    {
        await using var db = CreateDbContext();
        var subscription = new HisWebhookSubscription
        {
            TenantId = "tenant-1",
            ApiClientId = "client-1",
            EndpointUrl = "https://example.com/webhook",
            SecretProtected = "protected",
            EventTypes = "clinical.calculation.completed"
        };
        var outboxEvent = new HisOutboxEvent
        {
            WebhookSubscription = subscription,
            TenantId = "tenant-1",
            ApiClientId = "client-1",
            ResultId = "calc-replay",
            PayloadJson = "{}",
            Status = HisOutboxStatus.DeadLetter,
            AttemptCount = 8,
            LastError = "HTTP 503",
            LockId = "stale-lock",
            LockedUntil = DateTime.UtcNow.AddMinutes(1)
        };
        db.Add(outboxEvent);
        await db.SaveChangesAsync();
        var metrics = new FakeMetrics();
        var service = new HisOutboxOperationsService(db, metrics);

        var result = await service.ReplayDeadLetterAsync(outboxEvent.Id);

        Assert.Equal(HisOutboxReplayResult.Replayed, result);
        Assert.Equal(HisOutboxStatus.Pending, outboxEvent.Status);
        Assert.Equal(0, outboxEvent.AttemptCount);
        Assert.Null(outboxEvent.LastError);
        Assert.Null(outboxEvent.LockId);
        Assert.Null(outboxEvent.LockedUntil);
        Assert.Equal(1, metrics.Count);
    }

    [Fact]
    public async Task ReplayDeadLetter_RejectsInactiveSubscription()
    {
        await using var db = CreateDbContext();
        var outboxEvent = new HisOutboxEvent
        {
            WebhookSubscription = new HisWebhookSubscription
            {
                TenantId = "tenant-1",
                ApiClientId = "client-1",
                EndpointUrl = "https://example.com/webhook",
                SecretProtected = "protected",
                EventTypes = "clinical.calculation.completed",
                IsActive = false
            },
            TenantId = "tenant-1",
            ApiClientId = "client-1",
            ResultId = "calc-replay",
            PayloadJson = "{}",
            Status = HisOutboxStatus.DeadLetter
        };
        db.Add(outboxEvent);
        await db.SaveChangesAsync();

        var result = await new HisOutboxOperationsService(db, new FakeMetrics())
            .ReplayDeadLetterAsync(outboxEvent.Id);

        Assert.Equal(HisOutboxReplayResult.SubscriptionInactive, result);
        Assert.Equal(HisOutboxStatus.DeadLetter, outboxEvent.Status);
    }

    [Fact]
    public async Task Sender_DeliversSignedPayloadThroughRealHttpsServer()
    {
        const string secret = "webhook-secret-at-least-32-characters";
        const string payload = "{\"resultId\":\"calc-real-http\"}";
        var received = new TaskCompletionSource<ReceivedWebhook>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var certificate = CreateCertificate();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
        var app = builder.Build();
        app.MapPost("/webhook", async context =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            received.TrySetResult(new ReceivedWebhook(
                await reader.ReadToEndAsync(),
                context.Request.Headers["X-BiliTool-Event-Id"].ToString(),
                context.Request.Headers["X-BiliTool-Event-Type"].ToString(),
                context.Request.Headers["X-BiliTool-Timestamp"].ToString(),
                context.Request.Headers["X-BiliTool-Signature"].ToString()));
            context.Response.StatusCode = 204;
        });
        await app.StartAsync();

        try
        {
            var address = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            var dataDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            var dataProtection = DataProtectionProvider.Create(dataDirectory);
            var subscription = new HisWebhookSubscription
            {
                TenantId = "tenant-1",
                ApiClientId = "client-1",
                EndpointUrl = $"{address}/webhook",
                SecretProtected = dataProtection.CreateProtector("BiliTool.Vn.HIS.WebhookSecret.v1").Protect(secret),
                EventTypes = "clinical.calculation.completed"
            };
            var outboxEvent = new HisOutboxEvent
            {
                WebhookSubscription = subscription,
                TenantId = "tenant-1",
                ApiClientId = "client-1",
                EventType = "clinical.calculation.completed",
                ResultId = "calc-real-http",
                PayloadJson = payload
            };
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                AllowAutoRedirect = false
            };
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Webhooks:AllowLoopback"] = "true" })
                .Build();
            var sender = new HisWebhookSender(
                new FixedHttpClientFactory(httpClient),
                dataProtection,
                configuration,
                new HisWebhookResilienceGate(configuration));

            var result = await sender.SendAsync(subscription, outboxEvent, CancellationToken.None);
            var delivered = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(result.Succeeded);
            Assert.Equal(payload, delivered.Payload);
            Assert.Equal(outboxEvent.Id.ToString("N"), delivered.EventId);
            Assert.Equal(outboxEvent.EventType, delivered.EventType);
            var timestamp = long.Parse(delivered.Timestamp, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(WebhookSignature.Create(secret, timestamp, payload), delivered.Signature);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static BiliToolDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BiliToolDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new BiliToolDbContext(options);
    }

    private sealed class FakeRequestContext : IClinicalRequestContext
    {
        public string? TenantIdValue { get; init; }
        public string? ApiClientIdValue { get; init; }
        public string? CorrelationIdValue { get; init; }
        public string? TenantId => TenantIdValue;
        public string? ApiClientId => ApiClientIdValue;
        public string? CorrelationId => CorrelationIdValue;
        public string? ResultId { get; set; }
    }

    private sealed class FakeMetrics : IHisIntegrationMetrics
    {
        public int Count { get; private set; }
        public void Increment(string eventName) => Count++;
    }

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed record ReceivedWebhook(string Payload, string EventId, string EventType, string Timestamp, string Signature);

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
    }
}

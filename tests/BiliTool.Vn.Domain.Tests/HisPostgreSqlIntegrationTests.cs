using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Infrastructure.Persistence;
using BiliTool.Vn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class HisPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("bilitool_tests")
        .WithUsername("bilitool")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public BiliToolDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BiliToolDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new BiliToolDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class HisPostgreSqlCollection : ICollectionFixture<HisPostgreSqlFixture>
{
    public const string Name = "HIS PostgreSQL integration";
}

[Collection(HisPostgreSqlCollection.Name)]
public sealed class HisPostgreSqlIntegrationTests(HisPostgreSqlFixture fixture)
{
    [Fact]
    public async Task ConcurrentAcquire_AllowsExactlyOneOwner()
    {
        var key = $"concurrent-{Guid.NewGuid():N}";
        await using var firstDb = fixture.CreateDbContext();
        await using var secondDb = fixture.CreateDbContext();
        var first = new HisIdempotencyService(firstDb);
        var second = new HisIdempotencyService(secondDb);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entrants = 0;

        async Task<HisIdempotencyAcquireResult> AcquireAsync(IHisIdempotencyService service)
        {
            if (Interlocked.Increment(ref entrants) == 2) ready.SetResult();
            await ready.Task;
            return await service.AcquireAsync("tenant-a", "client-a", key, "HASH-A");
        }

        var decisions = await Task.WhenAll(AcquireAsync(first), AcquireAsync(second));

        Assert.Equal(1, decisions.Count(item => item.Decision == HisIdempotencyDecision.Acquired));
        Assert.Equal(1, decisions.Count(item => item.Decision == HisIdempotencyDecision.InProgress));
    }

    [Fact]
    public async Task SameKey_IsIsolatedAcrossTenantAndClientBoundary()
    {
        var key = $"tenant-isolation-{Guid.NewGuid():N}";
        await using var firstDb = fixture.CreateDbContext();
        await using var secondDb = fixture.CreateDbContext();

        var first = await new HisIdempotencyService(firstDb)
            .AcquireAsync("tenant-a", "client-a", key, "HASH-A");
        var second = await new HisIdempotencyService(secondDb)
            .AcquireAsync("tenant-b", "client-b", key, "HASH-B");

        Assert.Equal(HisIdempotencyDecision.Acquired, first.Decision);
        Assert.Equal(HisIdempotencyDecision.Acquired, second.Decision);
        await using var verificationDb = fixture.CreateDbContext();
        Assert.Equal(2, await verificationDb.HisIdempotencyRecords.CountAsync(item => item.IdempotencyKey == key));
    }

    [Fact]
    public async Task CompletedHl7Payload_ReplaysOpaqueBodyAndContentType()
    {
        var key = $"hl7-replay-{Guid.NewGuid():N}";
        const string payload = "MSH|^~\\&|BILITOOL|FAC|HIS|FAC|20260801120000+0000||ACK^R01|ACK-1|P|2.5.1\rMSA|AA|MSG-1\rZBR|calc_1";
        await using var db = fixture.CreateDbContext();
        var service = new HisIdempotencyService(db);

        var acquired = await service.AcquireAsync("tenant-hl7", "client-hl7", key, "HASH-HL7");
        Assert.Equal(HisIdempotencyDecision.Acquired, acquired.Decision);
        await service.CompleteAsync(
            "tenant-hl7",
            "client-hl7",
            key,
            "calc_1",
            200,
            payload,
            "application/hl7-v2; charset=utf-8");

        db.ChangeTracker.Clear();
        var replay = await service.AcquireAsync("tenant-hl7", "client-hl7", key, "HASH-HL7");

        Assert.Equal(HisIdempotencyDecision.Replay, replay.Decision);
        Assert.Equal(payload, replay.ResponseJson);
        Assert.Equal("application/hl7-v2; charset=utf-8", replay.ResponseContentType);
    }

    [Fact]
    public async Task Retention_DryRunAndLegalHoldProduceAuditableCounts()
    {
        var tenantId = $"tenant-{Guid.NewGuid():N}"[..32];
        var protectedResultId = $"calc_{Guid.NewGuid():N}";
        var eligibleResultId = $"calc_{Guid.NewGuid():N}";
        await using var db = fixture.CreateDbContext();
        var protectedAudit = NewAudit(tenantId, protectedResultId);
        var eligibleAudit = NewAudit(tenantId, eligibleResultId);
        db.AddRange(protectedAudit, eligibleAudit);
        await db.SaveChangesAsync();
        var oldTimestamp = DateTime.UtcNow.AddDays(-200);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE clinical_audit_logs SET calculated_at = {oldTimestamp} WHERE id IN ({protectedAudit.Id}, {eligibleAudit.Id})");
        db.ChangeTracker.Clear();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Audit:ClinicalRetentionDays"] = "180" })
            .Build();
        var governance = new ClinicalAuditGovernanceService(db, configuration);
        await governance.PlaceLegalHoldAsync(tenantId, protectedResultId, "Điều tra an toàn lâm sàng", "admin-1");

        var dryRun = await governance.RunRetentionAsync(true);

        Assert.Equal(1, dryRun.EligibleCount);
        Assert.Equal(1, dryRun.ProtectedByLegalHoldCount);
        Assert.Equal(0, dryRun.DeletedCount);
        Assert.Equal(2, await db.ClinicalAuditLogs.CountAsync(item => item.TenantId == tenantId));

        var purge = await governance.RunRetentionAsync(false);

        Assert.Equal(1, purge.DeletedCount);
        Assert.True(await db.ClinicalAuditLogs.AnyAsync(item => item.Id == protectedAudit.Id));
        Assert.False(await db.ClinicalAuditLogs.AnyAsync(item => item.Id == eligibleAudit.Id));
        Assert.Equal(2, await db.ClinicalAuditPurgeReports.CountAsync(item => item.Id == dryRun.ReportId || item.Id == purge.ReportId));
    }

    [Fact]
    public async Task ClinicalAudit_PersistsRequestIdentityProvenanceAndRedaction()
    {
        var tenantId = $"tenant-{Guid.NewGuid():N}"[..32];
        var clientId = $"client-{Guid.NewGuid():N}"[..32];
        var resultId = $"calc_{Guid.NewGuid():N}";
        var correlationId = $"corr_{Guid.NewGuid():N}";
        await using var db = fixture.CreateDbContext();
        var context = new TestClinicalRequestContext(tenantId, clientId, correlationId, resultId);
        var service = new ClinicalAuditService(db, NullLogger<ClinicalAuditService>.Instance, context);
        var trace = new BilirubinCalculationTrace
        {
            TuoiGio = 48,
            TuoiThaiTuan = 38,
            BilirubinMgDl = 12m
        };

        await service.TryRecordCalculationAsync(
            new
            {
                PatientIdentifier = "PATIENT-SECRET",
                EncounterIdentifier = "ENCOUNTER-SECRET",
                Bilirubin = 12m
            },
            new { ResultId = resultId, Recommendation = "follow-up" },
            trace);

        db.ChangeTracker.Clear();
        var audit = await db.ClinicalAuditLogs.SingleAsync(item => item.ResultId == resultId);
        Assert.Equal(tenantId, audit.TenantId);
        Assert.Equal(clientId, audit.ApiClientId);
        Assert.Equal(correlationId, audit.CorrelationId);
        Assert.Equal(BilirubinEngineMetadata.EngineVersion, audit.EngineVersion);
        Assert.Contains("[REDACTED]", audit.RequestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("PATIENT-SECRET", audit.RequestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("ENCOUNTER-SECRET", audit.RequestJson, StringComparison.Ordinal);
        Assert.Contains("12", audit.RequestJson, StringComparison.Ordinal);
    }

    private static BiliTool.Vn.Domain.Entities.ClinicalAuditLog NewAudit(string tenantId, string resultId) => new()
    {
        TenantId = tenantId,
        ResultId = resultId,
        RequestJson = "{}",
        ResponseJson = "{}",
        TraceJson = "{}"
    };

    private sealed class TestClinicalRequestContext(
        string tenantId,
        string apiClientId,
        string correlationId,
        string resultId) : IClinicalRequestContext
    {
        public string? TenantId => tenantId;
        public string? ApiClientId => apiClientId;
        public string? CorrelationId => correlationId;
        public string? ResultId { get; set; } = resultId;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BiliTool.Vn.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using BiliTool.Vn.Application.Services;
using System.Diagnostics;

namespace BiliTool.Vn.Infrastructure.Services;

public record HisWebhookDeliveryResult(bool Succeeded, int? StatusCode, string? Error);

public sealed class HisWebhookSender(
    IHttpClientFactory httpClientFactory,
    IDataProtectionProvider dataProtectionProvider,
    IConfiguration configuration,
    HisWebhookResilienceGate resilienceGate)
{
    public async Task<HisWebhookDeliveryResult> SendAsync(
        HisWebhookSubscription subscription,
        HisOutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        using var activity = HisIntegrationDiagnostics.ActivitySource.StartActivity("his.webhook.deliver", ActivityKind.Client);
        activity?.SetTag("his.tenant_id", outboxEvent.TenantId);
        activity?.SetTag("his.api_client_id", outboxEvent.ApiClientId);
        activity?.SetTag("his.event_type", outboxEvent.EventType);
        activity?.SetTag("his.outbox_attempt", outboxEvent.AttemptCount + 1);
        var endpoint = new Uri(subscription.EndpointUrl, UriKind.Absolute);
        if (!await IsAllowedEndpointAsync(endpoint, cancellationToken))
            return new HisWebhookDeliveryResult(false, null, "Webhook endpoint bị chặn bởi network policy.");

        string secret;
        try
        {
            secret = dataProtectionProvider.CreateProtector(HisWebhookSecretProtection.Purpose)
                .Unprotect(subscription.SecretProtected);
        }
        catch (Exception ex)
        {
            return new HisWebhookDeliveryResult(false, null, $"Không thể giải mã webhook secret: {ex.GetType().Name}");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(outboxEvent.PayloadJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-BiliTool-Event-Id", outboxEvent.Id.ToString("N"));
        request.Headers.Add("X-BiliTool-Event-Type", outboxEvent.EventType);
        request.Headers.Add("X-BiliTool-Timestamp", timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.Add("X-BiliTool-Signature", WebhookSignature.Create(secret, timestamp, outboxEvent.PayloadJson));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return await resilienceGate.ExecuteAsync(async () =>
        {
            try
            {
                using var response = await httpClientFactory.CreateClient("HisWebhook").SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return new HisWebhookDeliveryResult(true, (int)response.StatusCode, null);
                }
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {(int)response.StatusCode}");
                return new HisWebhookDeliveryResult(false, (int)response.StatusCode, $"HTTP {(int)response.StatusCode}");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new HisWebhookDeliveryResult(false, null, "Webhook timeout.");
            }
            catch (HttpRequestException ex)
            {
                return new HisWebhookDeliveryResult(false, null, ex.Message);
            }
        }, cancellationToken);
    }

    private async Task<bool> IsAllowedEndpointAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        if (endpoint.Scheme != Uri.UriSchemeHttps) return false;
        if (endpoint.IsLoopback) return configuration.GetValue("Webhooks:AllowLoopback", false);
        if (configuration.GetValue("Webhooks:AllowPrivateNetworks", false)) return true;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(endpoint.DnsSafeHost, cancellationToken);
        }
        catch
        {
            return false;
        }
        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] != 10 &&
                   !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31) &&
                   !(bytes[0] == 192 && bytes[1] == 168) &&
                   !(bytes[0] == 169 && bytes[1] == 254) &&
                   bytes[0] != 127 &&
                   bytes[0] != 0;
        }

        return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !address.IsIPv6Multicast &&
               !address.Equals(IPAddress.IPv6Loopback) &&
               (address.GetAddressBytes()[0] & 0xFE) != 0xFC;
    }
}

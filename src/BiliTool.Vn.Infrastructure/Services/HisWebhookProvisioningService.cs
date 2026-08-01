using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisWebhookProvisioningService(
    BiliToolDbContext dbContext,
    IDataProtectionProvider dataProtectionProvider) : IHisWebhookProvisioningService
{
    public async Task ConfigureAsync(
        string tenantId,
        string apiClientId,
        Uri endpoint,
        string secret,
        IReadOnlyCollection<string> eventTypes,
        CancellationToken cancellationToken = default)
    {
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Webhook endpoint phải dùng HTTPS tuyệt đối.", nameof(endpoint));
        if (secret.Length < 32) throw new ArgumentException("Webhook secret phải có ít nhất 32 ký tự.", nameof(secret));
        if (eventTypes.Count == 0) throw new ArgumentException("Phải đăng ký ít nhất một event type.", nameof(eventTypes));

        var subscription = await dbContext.HisWebhookSubscriptions.SingleOrDefaultAsync(
            item => item.TenantId == tenantId && item.ApiClientId == apiClientId && item.EndpointUrl == endpoint.ToString(),
            cancellationToken);
        if (subscription == null)
        {
            subscription = new HisWebhookSubscription
            {
                TenantId = tenantId,
                ApiClientId = apiClientId,
                EndpointUrl = endpoint.ToString()
            };
            dbContext.HisWebhookSubscriptions.Add(subscription);
        }

        subscription.SecretProtected = dataProtectionProvider
            .CreateProtector(HisWebhookSecretProtection.Purpose)
            .Protect(secret);
        subscription.EventTypes = string.Join(' ', eventTypes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        subscription.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DisableAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await dbContext.HisWebhookSubscriptions.FindAsync([subscriptionId], cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy webhook subscription.");
        subscription.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

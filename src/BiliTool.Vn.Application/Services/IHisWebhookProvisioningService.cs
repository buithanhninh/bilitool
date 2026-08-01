namespace BiliTool.Vn.Application.Services;

public interface IHisWebhookProvisioningService
{
    Task ConfigureAsync(
        string tenantId,
        string apiClientId,
        Uri endpoint,
        string secret,
        IReadOnlyCollection<string> eventTypes,
        CancellationToken cancellationToken = default);

    Task DisableAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}

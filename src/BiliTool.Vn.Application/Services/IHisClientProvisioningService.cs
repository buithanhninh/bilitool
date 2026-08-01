namespace BiliTool.Vn.Application.Services;

public record HisClientProvisioningRequest(
    string TenantCode,
    string TenantName,
    string ClientCode,
    string DisplayName,
    string ApiKey,
    string Scopes,
    DateTime? ExpiresAt = null,
    bool RequireMutualTls = false,
    string? CertificateFingerprint = null);

public interface IHisClientProvisioningService
{
    Task ProvisionAsync(HisClientProvisioningRequest request, CancellationToken cancellationToken = default);
    Task RotateKeyAsync(
        string tenantCode,
        string clientCode,
        string newApiKey,
        TimeSpan overlapWindow,
        CancellationToken cancellationToken = default);
    Task RotateCertificateAsync(
        string tenantCode,
        string clientCode,
        string newCertificateFingerprint,
        TimeSpan overlapWindow,
        CancellationToken cancellationToken = default);
    Task RevokeAsync(string tenantCode, string clientCode, CancellationToken cancellationToken = default);
}

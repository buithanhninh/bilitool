namespace BiliTool.Vn.Application.Services;

public record HisApiClientIdentity(
    string TenantId,
    string TenantCode,
    string ApiClientId,
    string ClientCode,
    IReadOnlySet<string> Scopes,
    bool IsLegacy);

public interface IHisApiClientAuthenticator
{
    Task<HisApiClientIdentity?> AuthenticateAsync(
        string apiKey,
        string? certificateFingerprint = null,
        CancellationToken cancellationToken = default);
}

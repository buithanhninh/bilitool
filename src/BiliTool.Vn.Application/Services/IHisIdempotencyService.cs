namespace BiliTool.Vn.Application.Services;

public enum HisIdempotencyDecision
{
    Acquired,
    Replay,
    PayloadConflict,
    InProgress
}

public record HisIdempotencyAcquireResult(
    HisIdempotencyDecision Decision,
    string? ResultId = null,
    int? ResponseStatusCode = null,
    string? ResponseJson = null,
    string? ResponseContentType = null);

public interface IHisIdempotencyService
{
    Task<HisIdempotencyAcquireResult> AcquireAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        string resultId,
        int responseStatusCode,
        string responseJson,
        string responseContentType,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        string tenantId,
        string apiClientId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

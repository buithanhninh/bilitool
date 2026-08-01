namespace BiliTool.Vn.Application.Services;

public sealed record ClinicalAuditPurgeResult(
    Guid ReportId,
    DateTime CutoffAt,
    bool DryRun,
    int EligibleCount,
    int ProtectedByLegalHoldCount,
    int DeletedCount);

public interface IClinicalAuditGovernanceService
{
    Task<Guid> PlaceLegalHoldAsync(string tenantId, string? resultId, string reason, string actorId, CancellationToken cancellationToken = default);
    Task<bool> ReleaseLegalHoldAsync(Guid holdId, string actorId, CancellationToken cancellationToken = default);
    Task<ClinicalAuditPurgeResult> RunRetentionAsync(bool dryRun, CancellationToken cancellationToken = default);
}

using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class ClinicalAuditGovernanceService(
    BiliToolDbContext db,
    IConfiguration configuration) : IClinicalAuditGovernanceService
{
    public async Task<Guid> PlaceLegalHoldAsync(
        string tenantId,
        string? resultId,
        string reason,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        tenantId = tenantId.Trim();
        resultId = string.IsNullOrWhiteSpace(resultId) ? null : resultId.Trim();
        reason = reason.Trim();
        actorId = actorId.Trim();
        if (tenantId.Length is < 2 or > 64) throw new ArgumentException("TenantId không hợp lệ.", nameof(tenantId));
        if (resultId?.Length > 64) throw new ArgumentException("ResultId không hợp lệ.", nameof(resultId));
        if (reason.Length is < 8 or > 1000) throw new ArgumentException("Lý do legal hold phải dài 8-1000 ký tự.", nameof(reason));
        if (actorId.Length is < 1 or > 256) throw new ArgumentException("ActorId không hợp lệ.", nameof(actorId));

        var duplicate = await db.ClinicalAuditLegalHolds.AnyAsync(
            hold => hold.TenantId == tenantId && hold.ResultId == resultId && hold.ReleasedAt == null,
            cancellationToken);
        if (duplicate) throw new InvalidOperationException("Legal hold đang active cho phạm vi này.");

        var hold = new ClinicalAuditLegalHold
        {
            TenantId = tenantId,
            ResultId = resultId,
            Reason = reason,
            PlacedBy = actorId
        };
        db.ClinicalAuditLegalHolds.Add(hold);
        await db.SaveChangesAsync(cancellationToken);
        return hold.Id;
    }

    public async Task<bool> ReleaseLegalHoldAsync(
        Guid holdId,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var hold = await db.ClinicalAuditLegalHolds.SingleOrDefaultAsync(
            item => item.Id == holdId && item.ReleasedAt == null,
            cancellationToken);
        if (hold is null) return false;
        hold.ReleasedBy = actorId.Trim();
        hold.ReleasedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ClinicalAuditPurgeResult> RunRetentionAsync(
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var retentionDays = Math.Max(30, configuration.GetValue("Audit:ClinicalRetentionDays", 180));
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var expired = db.ClinicalAuditLogs.Where(audit => audit.CalculatedAt < cutoff);
        var protectedQuery = expired.Where(audit => db.ClinicalAuditLegalHolds.Any(hold =>
            hold.ReleasedAt == null &&
            hold.TenantId == audit.TenantId &&
            (hold.ResultId == null || hold.ResultId == audit.ResultId)));
        var eligibleQuery = expired.Where(audit => !db.ClinicalAuditLegalHolds.Any(hold =>
            hold.ReleasedAt == null &&
            hold.TenantId == audit.TenantId &&
            (hold.ResultId == null || hold.ResultId == audit.ResultId)));

        var protectedCount = await protectedQuery.CountAsync(cancellationToken);
        var eligibleCount = await eligibleQuery.CountAsync(cancellationToken);
        var deletedCount = dryRun ? 0 : await eligibleQuery.ExecuteDeleteAsync(cancellationToken);
        var report = new ClinicalAuditPurgeReport
        {
            CutoffAt = cutoff,
            DryRun = dryRun,
            EligibleCount = eligibleCount,
            ProtectedByLegalHoldCount = protectedCount,
            DeletedCount = deletedCount
        };
        db.ClinicalAuditPurgeReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        return new ClinicalAuditPurgeResult(report.Id, cutoff, dryRun, eligibleCount, protectedCount, deletedCount);
    }
}

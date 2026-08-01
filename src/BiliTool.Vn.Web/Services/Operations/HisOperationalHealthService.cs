using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Domain.ValueObjects;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Web.Services.Operations;

public sealed record HisOperationalHealthSnapshot(
    bool Ready,
    string Database,
    string ClinicalEngine,
    int PendingOutbox,
    int DeadLetterOutbox,
    DateTime? OldestPendingAt,
    int ActiveApiClients,
    int ActiveWebhookSubscriptions,
    DateTime? LatestClinicalAuditAt,
    DateTimeOffset CheckedAt,
    string? Error = null);

public sealed class HisOperationalHealthService(
    BiliToolDbContext dbContext,
    IBilirubinClinicalFacade clinicalFacade,
    ILogger<HisOperationalHealthService> logger)
{
    public async Task<HisOperationalHealthSnapshot> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                return Failed("Database unavailable");

            var clinical = clinicalFacade.TinhToan(
                48,
                10m,
                38,
                YeuToNguyCoThanKinh.KhongCoNguyCoThanKinh);
            if (clinical.KetQua.NguongChieuDen <= 0 || clinical.Trace.EngineVersion != BilirubinEngineMetadata.EngineVersion)
                return Failed("Clinical engine smoke check failed");

            var pending = await dbContext.HisOutboxEvents.CountAsync(
                item => item.Status == HisOutboxStatus.Pending || item.Status == HisOutboxStatus.Processing,
                cancellationToken);
            var deadLetter = await dbContext.HisOutboxEvents.CountAsync(
                item => item.Status == HisOutboxStatus.DeadLetter,
                cancellationToken);
            var oldestPending = await dbContext.HisOutboxEvents
                .Where(item => item.Status == HisOutboxStatus.Pending || item.Status == HisOutboxStatus.Processing)
                .Select(item => (DateTime?)item.CreatedAt)
                .MinAsync(cancellationToken);
            var activeClients = await dbContext.HisApiClients.CountAsync(
                item => item.IsActive && item.Tenant.IsActive,
                cancellationToken);
            var activeSubscriptions = await dbContext.HisWebhookSubscriptions.CountAsync(
                item => item.IsActive,
                cancellationToken);
            var latestAudit = await dbContext.ClinicalAuditLogs
                .Select(item => (DateTime?)item.CalculatedAt)
                .MaxAsync(cancellationToken);

            return new HisOperationalHealthSnapshot(
                true,
                "Ready",
                BilirubinEngineMetadata.EngineVersion,
                pending,
                deadLetter,
                oldestPending,
                activeClients,
                activeSubscriptions,
                latestAudit,
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HIS operational health check thất bại.");
            return Failed(ex.GetType().Name);
        }
    }

    private static HisOperationalHealthSnapshot Failed(string error) => new(
        false,
        "Unavailable",
        "Unknown",
        0,
        0,
        null,
        0,
        0,
        null,
        DateTimeOffset.UtcNow,
        error);
}

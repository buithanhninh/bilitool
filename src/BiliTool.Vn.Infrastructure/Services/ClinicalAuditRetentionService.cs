using BiliTool.Vn.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class ClinicalAuditRetentionService(IServiceScopeFactory scopeFactory, ILogger<ClinicalAuditRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Không thể dọn clinical audit log. Worker sẽ thử lại sau.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var governance = scope.ServiceProvider.GetRequiredService<IClinicalAuditGovernanceService>();
        var report = await governance.RunRetentionAsync(false, cancellationToken);
        if (report.DeletedCount > 0 || report.ProtectedByLegalHoldCount > 0)
            logger.LogInformation(
                "Clinical audit retention report {ReportId}: eligible={Eligible}, protected={Protected}, deleted={Deleted}",
                report.ReportId,
                report.EligibleCount,
                report.ProtectedByLegalHoldCount,
                report.DeletedCount);

        var db = scope.ServiceProvider.GetRequiredService<BiliTool.Vn.Infrastructure.Persistence.BiliToolDbContext>();
        var expiredIdempotencyRecords = await db.HisIdempotencyRecords
            .Where(record => record.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
        if (expiredIdempotencyRecords > 0)
            logger.LogInformation("Đã xóa {Count} idempotency record hết hạn", expiredIdempotencyRecords);
    }
}

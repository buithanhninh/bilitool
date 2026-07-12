using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class ClinicalAuditRetentionService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ClinicalAuditRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PurgeExpiredAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var retentionDays = configuration.GetValue("Audit:ClinicalRetentionDays", 180);
        if (retentionDays < 30) retentionDays = 30;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BiliToolDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var deleted = await db.ClinicalAuditLogs.Where(x => x.CalculatedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
        if (deleted > 0) logger.LogInformation("Đã xóa {Count} clinical audit log quá hạn retention", deleted);
    }
}

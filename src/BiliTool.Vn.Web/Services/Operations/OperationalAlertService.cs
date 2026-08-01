using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Web.Services.Operations;

public sealed class OperationalAlertService(
    OperationalMetrics metrics,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OperationalAlertService> logger) : BackgroundService
{
    private DateTime _lastAlertAt = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operational alert evaluation thất bại.");
            }

            var intervalSeconds = Math.Clamp(configuration.GetValue("Operations:AlertEvaluationIntervalSeconds", 60), 5, 3600);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    internal async Task<bool> EvaluateOnceAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = metrics.GetMonitoringSnapshot();
        var p95Threshold = configuration.GetValue("Operations:AlertP95Milliseconds", 2000);
        var errorThreshold = configuration.GetValue("Operations:AlertErrorRatePercent", 2d);
        var minimumRequests = configuration.GetValue("Operations:AlertMinimumRequests", 10);
        var queue = await GetOutboxSnapshotAsync(cancellationToken);
        var pendingThreshold = configuration.GetValue("Operations:AlertPendingOutbox", 100);
        var deadLetterThreshold = configuration.GetValue("Operations:AlertDeadLetterOutbox", 1);
        var oldestMinutesThreshold = configuration.GetValue("Operations:AlertOldestPendingMinutes", 10);

        var requestAlert = snapshot.Requests >= minimumRequests &&
                           (snapshot.P95Milliseconds > p95Threshold || snapshot.ErrorRatePercent > errorThreshold);
        var outboxAlert = queue.Pending >= pendingThreshold ||
                          queue.DeadLetter >= deadLetterThreshold ||
                          queue.OldestPendingMinutes >= oldestMinutesThreshold;
        var cooldown = TimeSpan.FromMinutes(Math.Max(0, configuration.GetValue("Operations:AlertCooldownMinutes", 15)));

        if ((!requestAlert && !outboxAlert) || DateTime.UtcNow - _lastAlertAt < cooldown) return false;

        _lastAlertAt = DateTime.UtcNow;
        logger.LogWarning(
            "Operational alert: requests={Requests}, p95Ms={P95Milliseconds}, errorRatePercent={ErrorRatePercent}, outboxPending={OutboxPending}, outboxDeadLetter={OutboxDeadLetter}, oldestPendingMinutes={OldestPendingMinutes}",
            snapshot.Requests,
            snapshot.P95Milliseconds,
            snapshot.ErrorRatePercent,
            queue.Pending,
            queue.DeadLetter,
            queue.OldestPendingMinutes);
        return true;
    }

    private async Task<OutboxAlertSnapshot> GetOutboxSnapshotAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BiliToolDbContext>();
        var pending = await db.HisOutboxEvents.CountAsync(
            item => item.Status == HisOutboxStatus.Pending || item.Status == HisOutboxStatus.Processing,
            cancellationToken);
        var deadLetter = await db.HisOutboxEvents.CountAsync(
            item => item.Status == HisOutboxStatus.DeadLetter,
            cancellationToken);
        var oldest = await db.HisOutboxEvents
            .Where(item => item.Status == HisOutboxStatus.Pending || item.Status == HisOutboxStatus.Processing)
            .Select(item => (DateTime?)item.CreatedAt)
            .MinAsync(cancellationToken);
        var oldestMinutes = oldest.HasValue
            ? Math.Max(0, (DateTime.UtcNow - oldest.Value).TotalMinutes)
            : 0;
        return new OutboxAlertSnapshot(pending, deadLetter, oldestMinutes);
    }

    private sealed record OutboxAlertSnapshot(int Pending, int DeadLetter, double OldestPendingMinutes);
}

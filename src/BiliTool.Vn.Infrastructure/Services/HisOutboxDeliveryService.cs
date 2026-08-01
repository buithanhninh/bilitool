using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BiliTool.Vn.Application.Services;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisOutboxDeliveryService(
    IServiceScopeFactory scopeFactory,
    IHisIntegrationMetrics metrics,
    ILogger<HisOutboxDeliveryService> logger) : BackgroundService
{
    private const int MaxAttempts = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                await Task.Delay(processed == 0 ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(250), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HisOutboxDeliveryService batch thất bại.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BiliToolDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<HisWebhookSender>();
        var now = DateTime.UtcNow;
        var ids = await db.HisOutboxEvents.AsNoTracking()
            .Where(item => item.NextAttemptAt <= now &&
                           (item.Status == HisOutboxStatus.Pending ||
                            (item.Status == HisOutboxStatus.Processing && item.LockedUntil < now)))
            .OrderBy(item => item.NextAttemptAt)
            .Select(item => item.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var lockId = Guid.NewGuid().ToString("N");
            var claimed = await db.HisOutboxEvents
                .Where(item => item.Id == id && item.NextAttemptAt <= now &&
                               (item.Status == HisOutboxStatus.Pending ||
                                (item.Status == HisOutboxStatus.Processing && item.LockedUntil < now)))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, HisOutboxStatus.Processing)
                    .SetProperty(item => item.LockId, lockId)
                    .SetProperty(item => item.LockedUntil, now.AddMinutes(2)), cancellationToken);
            if (claimed == 0) continue;

            var outboxEvent = await db.HisOutboxEvents
                .Include(item => item.WebhookSubscription)
                .SingleAsync(item => item.Id == id && item.LockId == lockId, cancellationToken);
            var result = outboxEvent.WebhookSubscription.IsActive
                ? await sender.SendAsync(outboxEvent.WebhookSubscription, outboxEvent, cancellationToken)
                : new HisWebhookDeliveryResult(false, null, "Webhook subscription đã bị vô hiệu hóa.");

            outboxEvent.AttemptCount++;
            outboxEvent.LockId = null;
            outboxEvent.LockedUntil = null;
            outboxEvent.LastError = Truncate(result.Error, 2000);
            if (result.Succeeded)
            {
                metrics.Increment("webhook.delivered");
                outboxEvent.Status = HisOutboxStatus.Delivered;
                outboxEvent.DeliveredAt = DateTime.UtcNow;
            }
            else if (outboxEvent.AttemptCount >= MaxAttempts || !outboxEvent.WebhookSubscription.IsActive)
            {
                metrics.Increment("webhook.dead_letter");
                outboxEvent.Status = HisOutboxStatus.DeadLetter;
            }
            else
            {
                metrics.Increment("webhook.retry_scheduled");
                outboxEvent.Status = HisOutboxStatus.Pending;
                outboxEvent.NextAttemptAt = DateTime.UtcNow.Add(CalculateBackoff(outboxEvent.AttemptCount));
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return ids.Count;
    }

    public static TimeSpan CalculateBackoff(int attemptCount) =>
        TimeSpan.FromSeconds(Math.Min(3600, 30 * Math.Pow(2, Math.Max(0, attemptCount - 1))));

    private static string? Truncate(string? value, int maxLength) =>
        value?.Length > maxLength ? value[..maxLength] : value;
}

using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Entities;
using BiliTool.Vn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BiliTool.Vn.Infrastructure.Services;

public sealed class HisOutboxOperationsService(
    BiliToolDbContext db,
    IHisIntegrationMetrics metrics) : IHisOutboxOperationsService
{
    public async Task<HisOutboxReplayResult> ReplayDeadLetterAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var outboxEvent = await db.HisOutboxEvents
            .Include(item => item.WebhookSubscription)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        if (outboxEvent is null) return HisOutboxReplayResult.NotFound;
        if (outboxEvent.Status != HisOutboxStatus.DeadLetter) return HisOutboxReplayResult.NotDeadLetter;
        if (!outboxEvent.WebhookSubscription.IsActive) return HisOutboxReplayResult.SubscriptionInactive;

        outboxEvent.Status = HisOutboxStatus.Pending;
        outboxEvent.AttemptCount = 0;
        outboxEvent.NextAttemptAt = DateTime.UtcNow;
        outboxEvent.DeliveredAt = null;
        outboxEvent.LastError = null;
        outboxEvent.LockId = null;
        outboxEvent.LockedUntil = null;
        await db.SaveChangesAsync(cancellationToken);
        metrics.Increment("webhook.dead_letter_replayed");
        return HisOutboxReplayResult.Replayed;
    }
}

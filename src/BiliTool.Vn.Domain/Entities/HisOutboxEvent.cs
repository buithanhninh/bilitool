namespace BiliTool.Vn.Domain.Entities;

public enum HisOutboxStatus
{
    Pending,
    Processing,
    Delivered,
    DeadLetter
}

public class HisOutboxEvent
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WebhookSubscriptionId { get; set; }
    public HisWebhookSubscription WebhookSubscription { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public string ApiClientId { get; set; } = string.Empty;
    public string EventType { get; set; } = "clinical.calculation.completed";
    public string ResultId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public HisOutboxStatus Status { get; set; } = HisOutboxStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public string? LastError { get; set; }
    public string? LockId { get; set; }
    public DateTime? LockedUntil { get; set; }
}

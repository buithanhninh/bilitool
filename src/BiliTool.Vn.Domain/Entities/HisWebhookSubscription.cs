namespace BiliTool.Vn.Domain.Entities;

public class HisWebhookSubscription
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string ApiClientId { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string SecretProtected { get; set; } = string.Empty;
    public string EventTypes { get; set; } = "clinical.calculation.completed";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public ICollection<HisOutboxEvent> OutboxEvents { get; set; } = new List<HisOutboxEvent>();
}

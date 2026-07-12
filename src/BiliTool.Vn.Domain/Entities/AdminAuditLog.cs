namespace BiliTool.Vn.Domain.Entities;

public class AdminAuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public bool Succeeded { get; set; }
    public string? RemoteIp { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
}

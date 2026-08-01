namespace BiliTool.Vn.Domain.Entities;

public sealed class ClinicalAuditLegalHold
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string? ResultId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PlacedBy { get; set; } = string.Empty;
    public DateTime PlacedAt { get; private set; } = DateTime.UtcNow;
    public string? ReleasedBy { get; set; }
    public DateTime? ReleasedAt { get; set; }
}

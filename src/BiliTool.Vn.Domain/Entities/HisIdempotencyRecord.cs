namespace BiliTool.Vn.Domain.Entities;

public enum HisIdempotencyStatus
{
    Pending,
    Completed
}

public class HisIdempotencyRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string ApiClientId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public HisIdempotencyStatus Status { get; set; } = HisIdempotencyStatus.Pending;
    public string? ResultId { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseJson { get; set; }
    public string? ResponseContentType { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}

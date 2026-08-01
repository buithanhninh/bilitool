namespace BiliTool.Vn.Domain.Entities;

public class HisApiClient
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public HisTenant Tenant { get; set; } = null!;
    public string ClientCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
    public byte[] ApiKeyHash { get; set; } = Array.Empty<byte>();
    public string? PreviousKeyFingerprint { get; set; }
    public byte[]? PreviousApiKeyHash { get; set; }
    public DateTime? PreviousKeyExpiresAt { get; set; }
    public bool RequireMutualTls { get; set; }
    public string? CertificateFingerprint { get; set; }
    public string? PreviousCertificateFingerprint { get; set; }
    public DateTime? PreviousCertificateExpiresAt { get; set; }
    public string Scopes { get; set; } = "bilirubin:calculate bilirubin:metadata";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}

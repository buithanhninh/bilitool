namespace BiliTool.Vn.Domain.Entities;

public class ClinicalAuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CalculatedAt { get; private set; } = DateTime.UtcNow;
    public string GuidelineCode { get; set; } = "AAP2022+NICECG98";
    public string EngineMode { get; set; } = "BaselineMayTinhBilirubin";
    public string? UserId { get; set; }
    public string? ApiClientId { get; set; }
    public string? CorrelationId { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public string TraceJson { get; set; } = string.Empty;
}

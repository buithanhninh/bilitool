namespace BiliTool.Vn.Domain.Entities;

public sealed class ClinicalAuditPurgeReport
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime ExecutedAt { get; private set; } = DateTime.UtcNow;
    public DateTime CutoffAt { get; set; }
    public bool DryRun { get; set; }
    public int EligibleCount { get; set; }
    public int ProtectedByLegalHoldCount { get; set; }
    public int DeletedCount { get; set; }
}

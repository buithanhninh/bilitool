namespace BiliTool.Vn.Domain.Entities;

public class HisTenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public ICollection<HisApiClient> ApiClients { get; set; } = new List<HisApiClient>();
}

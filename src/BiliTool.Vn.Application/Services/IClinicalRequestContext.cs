namespace BiliTool.Vn.Application.Services;

public interface IClinicalRequestContext
{
    string? TenantId { get; }
    string? ApiClientId { get; }
    string? CorrelationId { get; }
    string? ResultId { get; set; }
}

using BiliTool.Vn.Application.Services;

namespace BiliTool.Vn.Web.Services;

public sealed class ClinicalRequestContext(IHttpContextAccessor httpContextAccessor) : IClinicalRequestContext
{
    public const string TenantIdItem = "HisTenantId";
    public const string TenantCodeItem = "HisTenantCode";
    public const string ApiClientIdItem = "HisApiClientId";
    public const string ResultIdItem = "HisResultId";

    private HttpContext? HttpContext => httpContextAccessor.HttpContext;

    public string? TenantId => HttpContext?.Items[TenantIdItem] as string;
    public string? ApiClientId => HttpContext?.Items[ApiClientIdItem] as string;
    public string? CorrelationId => HttpContext?.TraceIdentifier;

    public string? ResultId
    {
        get => HttpContext?.Items[ResultIdItem] as string;
        set
        {
            if (HttpContext != null) HttpContext.Items[ResultIdItem] = value;
        }
    }
}

using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BiliTool.Vn.Web.Filters;

public sealed class HisRolloutFilter(
    IConfiguration configuration,
    IHisIntegrationMetrics metrics) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantCode = context.HttpContext.Items[ClinicalRequestContext.TenantCodeItem] as string;
        if (string.IsNullOrWhiteSpace(tenantCode))
        {
            context.Result = Problem(context, "rollout_identity_missing", "Không xác định được tenant rollout.");
            return;
        }

        var disabled = configuration.GetSection("HisRollout:DisabledTenants").Get<string[]>() ?? [];
        var enabled = configuration.GetSection("HisRollout:EnabledTenants").Get<string[]>() ?? [];
        var globallyEnabled = configuration.GetValue("HisRollout:V3Enabled", true);
        var emergencyStop = configuration.GetValue("HisRollout:EmergencyKillSwitch", false);
        var allowed = globallyEnabled && !emergencyStop &&
                      !disabled.Contains(tenantCode, StringComparer.OrdinalIgnoreCase) &&
                      (enabled.Length == 0 || enabled.Contains(tenantCode, StringComparer.OrdinalIgnoreCase));
        if (!allowed)
        {
            metrics.Increment("rollout.tenant_blocked");
            context.HttpContext.Response.Headers.RetryAfter = "60";
            context.Result = Problem(context, "tenant_rollout_disabled", "API v3 chưa được mở cho tenant này hoặc đang bị emergency stop.");
            return;
        }

        await next();
    }

    private static ObjectResult Problem(ActionExecutingContext context, string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://bilitool.vn/problems/{errorCode}",
            Title = errorCode,
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = detail,
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        problem.Extensions["retryable"] = true;
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    }
}

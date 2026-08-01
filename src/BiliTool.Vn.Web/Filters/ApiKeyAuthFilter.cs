using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Web.Services;
using System.Security.Cryptography;

namespace BiliTool.Vn.Web.Filters;

/// <summary>
/// Bộ lọc hành động xác thực API Key thông qua Header X-API-Key phục vụ tích hợp HIS.
/// </summary>
public class ApiKeyAuthFilter : IAsyncActionFilter
{
    private readonly IHisApiClientAuthenticator _authenticator;
    private readonly IHisIntegrationMetrics _metrics;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthFilter(IHisApiClientAuthenticator authenticator, IHisIntegrationMetrics metrics)
    {
        _authenticator = authenticator;
        _metrics = metrics;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Kiểm tra xem Header X-API-Key có được gửi lên không
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _metrics.Increment("auth.missing_key");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "Yêu cầu API Key (X-API-Key is missing)",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "Thiếu API Key để truy cập tài nguyên này. Vui lòng gửi kèm Header 'X-API-Key'."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            AddProblemExtensions((context.Result as ObjectResult)!, "missing_api_key", context.HttpContext.TraceIdentifier, false);
            return;
        }

        // Trích xuất và cắt khoảng trắng thừa (nếu có) để tránh lỗi sao chép thừa khoảng trắng
        var keyToCheck = extractedApiKey.ToString().Trim();

        HisApiClientIdentity? identity;
        try
        {
            var certificate = await context.HttpContext.Connection.GetClientCertificateAsync(context.HttpContext.RequestAborted);
            var certificateFingerprint = certificate == null ? null : Convert.ToHexString(SHA256.HashData(certificate.RawData));
            identity = await _authenticator.AuthenticateAsync(keyToCheck, certificateFingerprint, context.HttpContext.RequestAborted);
        }
        catch
        {
            _metrics.Increment("auth.unavailable");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "Dịch vụ xác thực HIS tạm thời không khả dụng",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Không thể xác thực hệ thống tích hợp tại thời điểm này."
            }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
            AddProblemExtensions((context.Result as ObjectResult)!, "authentication_unavailable", context.HttpContext.TraceIdentifier, true);
            return;
        }

        if (identity == null)
        {
            _metrics.Increment("auth.invalid_key");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                Title = "API Key không hợp lệ (Unauthorized client)",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "API Key được cung cấp không chính xác, bị bỏ trống hoặc không có quyền truy cập."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            AddProblemExtensions((context.Result as ObjectResult)!, "invalid_api_key", context.HttpContext.TraceIdentifier, false);
            return;
        }

        var requiredScope = context.HttpContext.Request.Method == HttpMethods.Get
            ? "bilirubin:metadata"
            : "bilirubin:calculate";
        if (!identity.Scopes.Contains(requiredScope))
        {
            _metrics.Increment("auth.insufficient_scope");
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "API client không có quyền truy cập",
                Status = StatusCodes.Status403Forbidden,
                Detail = $"Credential không có scope bắt buộc '{requiredScope}'."
            }) { StatusCode = StatusCodes.Status403Forbidden };
            AddProblemExtensions((context.Result as ObjectResult)!, "insufficient_scope", context.HttpContext.TraceIdentifier, false);
            return;
        }

        context.HttpContext.Items[ClinicalRequestContext.TenantIdItem] = identity.TenantId;
        context.HttpContext.Items[ClinicalRequestContext.TenantCodeItem] = identity.TenantCode;
        context.HttpContext.Items[ClinicalRequestContext.ApiClientIdItem] = identity.ApiClientId;
        _metrics.Increment(identity.IsLegacy ? "auth.success.legacy" : "auth.success.registry");

        await next();
    }

    private static void AddProblemExtensions(ObjectResult result, string errorCode, string correlationId, bool retryable)
    {
        if (result.Value is not ProblemDetails problem) return;
        problem.Type = $"https://bilitool.vn/problems/{errorCode}";
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["retryable"] = retryable;
    }
}

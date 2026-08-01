using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BiliTool.Vn.Web.Filters;

public sealed class HisIdempotencyFilter(
    IHisIdempotencyService idempotencyService,
    IClinicalRequestContext requestContext,
    IHisIntegrationMetrics metrics,
    Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> mvcJsonOptions) : IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private readonly JsonSerializerOptions _jsonOptions = new(mvcJsonOptions.Value.JsonSerializerOptions)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].ToString().Trim();
        if (!IsValidKey(key))
        {
            context.Result = Problem(
                StatusCodes.Status400BadRequest,
                "invalid_idempotency_key",
                "Idempotency-Key không hợp lệ",
                "Header Idempotency-Key bắt buộc, dài 8-128 ký tự và chỉ gồm chữ, số, '-', '_' hoặc '.'.",
                context.HttpContext.TraceIdentifier);
            return;
        }

        var tenantId = requestContext.TenantId;
        var apiClientId = requestContext.ApiClientId;
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(apiClientId))
        {
            context.Result = Problem(
                StatusCodes.Status500InternalServerError,
                "missing_integration_identity",
                "Thiếu integration identity",
                "Không thể xác định tenant hoặc API client cho request.",
                context.HttpContext.TraceIdentifier);
            return;
        }

        var requestHash = HashRequest(context);
        var acquisition = await idempotencyService.AcquireAsync(
            tenantId,
            apiClientId,
            key,
            requestHash,
            context.HttpContext.RequestAborted);

        switch (acquisition.Decision)
        {
            case HisIdempotencyDecision.PayloadConflict:
                metrics.Increment("idempotency.payload_conflict");
                context.Result = Problem(
                    StatusCodes.Status409Conflict,
                    "idempotency_payload_conflict",
                    "Idempotency-Key đã dùng cho payload khác",
                    "Dùng Idempotency-Key mới khi nội dung request thay đổi.",
                    context.HttpContext.TraceIdentifier);
                return;
            case HisIdempotencyDecision.InProgress:
                metrics.Increment("idempotency.in_progress");
                context.HttpContext.Response.Headers.RetryAfter = "1";
                context.Result = Problem(
                    StatusCodes.Status409Conflict,
                    "idempotency_request_in_progress",
                    "Request cùng Idempotency-Key đang được xử lý",
                    "Thử lại sau với cùng Idempotency-Key.",
                    context.HttpContext.TraceIdentifier);
                return;
            case HisIdempotencyDecision.Replay:
                metrics.Increment("idempotency.replay");
                requestContext.ResultId = acquisition.ResultId;
                context.HttpContext.Response.Headers["Idempotency-Replayed"] = "true";
                context.Result = new ContentResult
                {
                    StatusCode = acquisition.ResponseStatusCode ?? StatusCodes.Status200OK,
                    ContentType = acquisition.ResponseContentType ?? "application/json; charset=utf-8",
                    Content = acquisition.ResponseJson ?? "null"
                };
                return;
        }

        metrics.Increment("idempotency.acquired");

        var executed = await next();
        var statusCode = GetStatusCode(executed.Result);
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            await idempotencyService.ReleaseAsync(
                tenantId,
                apiClientId,
                key,
                context.HttpContext.RequestAborted);
            return;
        }

        var responseJson = SerializeResult(executed.Result);
        await idempotencyService.CompleteAsync(
            tenantId,
            apiClientId,
            key,
            requestContext.ResultId ?? string.Empty,
            statusCode,
            responseJson,
            GetContentType(context.HttpContext, executed.Result),
            context.HttpContext.RequestAborted);
    }

    private string HashRequest(ActionExecutingContext context)
    {
        var businessArguments = context.ActionArguments
            .Where(argument => argument.Value is not CancellationToken)
            .OrderBy(argument => argument.Key, StringComparer.Ordinal)
            .ToDictionary(argument => argument.Key, argument => argument.Value, StringComparer.Ordinal);
        var canonical = JsonSerializer.Serialize(businessArguments, _jsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{context.HttpContext.Request.Path}\n{canonical}"));
        return Convert.ToHexString(bytes);
    }

    private string SerializeResult(IActionResult? result) => result switch
    {
        ObjectResult objectResult => JsonSerializer.Serialize(objectResult.Value, _jsonOptions),
        JsonResult jsonResult => JsonSerializer.Serialize(jsonResult.Value, _jsonOptions),
        ContentResult contentResult => contentResult.Content ?? "null",
        EmptyResult => "null",
        _ => JsonSerializer.Serialize(new { }, _jsonOptions)
    };

    private static int GetStatusCode(IActionResult? result) => result switch
    {
        ObjectResult objectResult => objectResult.StatusCode ?? StatusCodes.Status200OK,
        StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
        JsonResult jsonResult => jsonResult.StatusCode ?? StatusCodes.Status200OK,
        ContentResult contentResult => contentResult.StatusCode ?? StatusCodes.Status200OK,
        _ => StatusCodes.Status200OK
    };

    private static string GetContentType(HttpContext httpContext, IActionResult? result) => result switch
    {
        ContentResult contentResult when !string.IsNullOrWhiteSpace(contentResult.ContentType) => contentResult.ContentType,
        ObjectResult objectResult when objectResult.ContentTypes.Count > 0 => objectResult.ContentTypes[0],
        _ when httpContext.Request.Path.StartsWithSegments("/api/v3/fhir/R4") => "application/fhir+json; charset=utf-8",
        _ when httpContext.Request.Path.StartsWithSegments("/api/v3/hl7/v251") => "application/hl7-v2; charset=utf-8",
        _ => "application/json; charset=utf-8"
    };

    private static ObjectResult Problem(int status, string errorCode, string title, string detail, string correlationId)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://bilitool.vn/problems/{errorCode}",
            Status = status,
            Title = title,
            Detail = detail
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["retryable"] = errorCode == "idempotency_request_in_progress";
        return new ObjectResult(problem) { StatusCode = status };
    }

    private static bool IsValidKey(string value)
    {
        if (value.Length is < 8 or > 128) return false;
        return value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.');
    }
}

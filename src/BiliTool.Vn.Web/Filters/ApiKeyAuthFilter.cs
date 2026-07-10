using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BiliTool.Vn.Web.Filters;

/// <summary>
/// Bộ lọc hành động xác thực API Key thông qua Header X-API-Key phục vụ tích hợp HIS.
/// </summary>
public class ApiKeyAuthFilter : IAsyncActionFilter
{
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Lấy danh sách khóa được cấu hình
        var allowedKeys = _configuration.GetSection("ApiSettings:AllowedApiKeys").Get<string[]>();

        // Không có cấu hình khóa nghĩa là tích hợp HIS chưa sẵn sàng; không mở public mặc định.
        if (allowedKeys == null || allowedKeys.Length == 0)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Dịch vụ API chưa được cấu hình (API key configuration missing)",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "API tích hợp HIS chưa được cấu hình khóa truy cập. Vui lòng liên hệ quản trị hệ thống."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        // Kiểm tra xem Header X-API-Key có được gửi lên không
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
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
            return;
        }

        // Trích xuất và cắt khoảng trắng thừa (nếu có) để tránh lỗi sao chép thừa khoảng trắng
        var keyToCheck = extractedApiKey.ToString().Trim();

        // Kiểm tra tính chính xác của API Key
        if (string.IsNullOrEmpty(keyToCheck) || !allowedKeys.Contains(keyToCheck))
        {
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
            return;
        }

        await next();
    }
}

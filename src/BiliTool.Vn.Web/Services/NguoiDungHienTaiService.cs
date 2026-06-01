using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BiliTool.Vn.Web.Services;

/// <summary>
/// Service quản lý thông tin người dùng hiện tại.
/// Hỗ trợ hai chế độ: đăng nhập Gmail hoặc ẩn danh.
/// 
/// Lưu ý Blazor Server: IHttpContextAccessor chỉ đọc được HttpContext
/// trong giai đoạn khởi tạo request (prerender). Dùng AuthenticationStateProvider
/// trong Blazor components để reactive với auth state.
/// </summary>
public class NguoiDungHienTaiService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public NguoiDungHienTaiService(IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }

    private ClaimsPrincipal? NguoiDung
        => _httpContextAccessor.HttpContext?.User;

    /// <summary>Người dùng đã xác thực (không phải ẩn danh)</summary>
    public bool DaDangNhap
        => NguoiDung?.Identity?.IsAuthenticated == true;

    /// <summary>Người dùng có phải là Quản trị viên không</summary>
    public bool LaQuanTriVien
    {
        get
        {
            if (!DaDangNhap) return false;
            
            // 1. Phân quyền cứng local administrator (qua Claims Role)
            if (NguoiDung?.IsInRole("Admin") == true) return true;

            // 2. Phân quyền Google OAuth (qua Emails)
            if (string.IsNullOrEmpty(Email)) return false;
            var adminEmails = _config.GetSection("AdminEmails").Get<string[]>();
            if (adminEmails == null) return false;
            return adminEmails.Contains(Email, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Tên hiển thị (Google display name)</summary>
    public string? TenHienThi
        => NguoiDung?.FindFirst(ClaimTypes.Name)?.Value
        ?? NguoiDung?.FindFirst("name")?.Value;

    /// <summary>Email đăng nhập Google</summary>
    public string? Email
        => NguoiDung?.FindFirst(ClaimTypes.Email)?.Value
        ?? NguoiDung?.FindFirst("email")?.Value;

    /// <summary>Google Subject ID (phân biệt người dùng)</summary>
    public string? Id
        => NguoiDung?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>URL avatar từ Google</summary>
    public string? AvatarUrl
        => NguoiDung?.FindFirst("picture")?.Value
        ?? NguoiDung?.FindFirst("urn:google:picture")?.Value;

    /// <summary>Chữ cái đầu tên để hiển thị avatar fallback</summary>
    public string ChuCaiDau
    {
        get
        {
            var ten = TenHienThi ?? Email;
            if (string.IsNullOrEmpty(ten)) return "?";
            return ten[0].ToString().ToUpper();
        }
    }

    /// <summary>Tên rút gọn để hiển thị trong sidebar</summary>
    public string TenRutGon
    {
        get
        {
            var ten = TenHienThi;
            if (string.IsNullOrEmpty(ten)) return Email?.Split('@')[0] ?? "Người dùng";
            // Lấy tên đầu tiên (first name)
            var parts = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts[0];
        }
    }
}

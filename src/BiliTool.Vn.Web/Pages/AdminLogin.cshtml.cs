using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BiliTool.Vn.Web.Security;
using BiliTool.Vn.Application.Services;

namespace BiliTool.Vn.Web.Pages;

public class AdminLoginModel : PageModel
{
    private readonly AdminCredentialVerifier _credentialVerifier;
    private readonly ILogger<AdminLoginModel> _logger;
    private readonly IAdminAuditService _adminAudit;

    public AdminLoginModel(AdminCredentialVerifier credentialVerifier, ILogger<AdminLoginModel> logger, IAdminAuditService adminAudit)
    {
        _credentialVerifier = credentialVerifier;
        _logger = logger;
        _adminAudit = adminAudit;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        // Nếu đã đăng nhập và là admin thì vào trang quản trị luôn
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return Redirect("/admin");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_credentialVerifier.IsConfigured)
        {
            _logger.LogCritical("AdminAuth chưa được cấu hình. Từ chối đăng nhập admin local.");
            ErrorMessage = "Đăng nhập quản trị tạm thời không khả dụng.";
            return Page();
        }

        if (_credentialVerifier.Verify(Username, Password))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Administrator"),
                new Claim(ClaimTypes.Email, "administrator@bilitool.vn"),
                new Claim(ClaimTypes.NameIdentifier, "admin_local"),
                new Claim(ClaimTypes.Role, "Admin")
                ,new Claim(ClaimTypes.Role, "SuperAdmin")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            _logger.LogWarning("Admin local đăng nhập thành công từ IP {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
            await _adminAudit.RecordAsync("admin_local", "administrator@bilitool.vn", "admin.login", "admin.session", null, true, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier);

            return Redirect("/admin");
        }

        await Task.Delay(Random.Shared.Next(250, 550));
        _logger.LogWarning("Đăng nhập admin thất bại từ IP {RemoteIp}", HttpContext.Connection.RemoteIpAddress);
        await _adminAudit.RecordAsync("unknown", string.Empty, "admin.login", "admin.session", null, false, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.TraceIdentifier);
        ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
        return Page();
    }
}

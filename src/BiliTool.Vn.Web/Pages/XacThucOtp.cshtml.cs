using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class XacThucOtpModel : PageModel
{
    private readonly IAuthService _authService;

    public XacThucOtpModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string OtpCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(OtpCode) || string.IsNullOrEmpty(Email))
        {
            ErrorMessage = "Vui lòng nhập mã OTP.";
            return Page();
        }

        var result = await _authService.VerifyOtpAsync(Email, OtpCode);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        TempData["SuccessMessage"] = "Xác thực email thành công. Bạn có thể đăng nhập.";
        return RedirectToPage("/DangNhap");
    }

    public async Task<IActionResult> OnPostResendAsync()
    {
        var result = await _authService.SendOtpAsync(Email);
        if (result.Success)
        {
            SuccessMessage = "Mã OTP mới đã được gửi đến email của bạn.";
        }
        else
        {
            ErrorMessage = result.Message;
        }
        return Page();
    }
}

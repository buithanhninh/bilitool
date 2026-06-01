using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class DatLaiMatKhauModel : PageModel
{
    private readonly IAuthService _authService;

    public DatLaiMatKhauModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string OtpCode { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(OtpCode) || string.IsNullOrEmpty(NewPassword))
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
            return Page();
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        if (NewPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return Page();
        }

        var result = await _authService.ResetPasswordAsync(Email, OtpCode, NewPassword);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
        return RedirectToPage("/DangNhap");
    }
}

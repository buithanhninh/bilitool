using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class DangKyModel : PageModel
{
    private readonly IAuthService _authService;

    public DangKyModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public RegisterRequest RegisterData { get; set; } = new();

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (RegisterData.Password != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        if (RegisterData.Password.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return Page();
        }

        var result = await _authService.RegisterAsync(RegisterData);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        // Redirect to OTP verification
        return RedirectToPage("/XacThucOtp", new { email = RegisterData.Email });
    }
}

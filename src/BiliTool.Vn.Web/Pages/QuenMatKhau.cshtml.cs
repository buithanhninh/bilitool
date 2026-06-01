using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class QuenMatKhauModel : PageModel
{
    private readonly IAuthService _authService;

    public QuenMatKhauModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email))
        {
            ErrorMessage = "Vui lòng nhập địa chỉ email.";
            return Page();
        }

        var result = await _authService.SendOtpAsync(Email, forPasswordReset: true);

        if (!result.Success)
        {
            ErrorMessage = result.Message;
            return Page();
        }

        // Redirect to Reset Password page
        return RedirectToPage("/DatLaiMatKhau", new { email = Email });
    }
}

using System.Security.Claims;
using BiliTool.Vn.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class DangNhapModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly IAuthService _authService;

    public DangNhapModel(IConfiguration config, IAuthService authService)
    {
        _config = config;
        _authService = authService;
    }

    public bool GoogleChuaCauHinh { get; private set; }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID")
        {
            GoogleChuaCauHinh = true;
        }
    }

    public IActionResult OnPostGoogle(string? returnUrl = null)
    {
        var clientId = _config["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID")
        {
            GoogleChuaCauHinh = true;
            return Page();
        }

        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        var props = new AuthenticationProperties
        {
            RedirectUri = redirectUri,
            Items = { { "returnUrl", redirectUri } }
        };

        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> OnPostLocalAsync(string? returnUrl = null)
    {
        OnGet(returnUrl);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _authService.LoginAsync(new LoginRequest { Email = Email, Password = Password });

        if (!result.Success)
        {
            if (result.Message.Contains("xác thực email"))
            {
                // Redirect to OTP page
                return RedirectToPage("/XacThucOtp", new { email = Email });
            }
            ErrorMessage = result.Message;
            return Page();
        }

        // Tạo Cookie xác thực
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId!),
            new Claim(ClaimTypes.Email, Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);

        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        return LocalRedirect(redirectUri);
    }
}

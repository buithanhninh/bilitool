using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

public class AdminLoginModel : PageModel
{
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
        if (Username == "administrator" && Password == "ThanhNinh@123")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Administrator"),
                new Claim(ClaimTypes.Email, "administrator@bilitool.vn"),
                new Claim(ClaimTypes.NameIdentifier, "admin_local"),
                new Claim(ClaimTypes.Role, "Admin")
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

            return Redirect("/admin");
        }

        ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
        return Page();
    }
}

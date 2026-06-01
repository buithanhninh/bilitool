using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BiliTool.Vn.Web.Pages;

/// <summary>
/// Trang đăng xuất.
/// Xóa authentication cookie và redirect về trang chủ.
/// </summary>
public class DangXuatModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        // Xóa cookie xác thực
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect về trang chủ
        return Redirect("/");
    }

    // Hỗ trợ POST (CSRF-safe) từ form đăng xuất
    public async Task<IActionResult> OnPostAsync()
        => await OnGetAsync();
}

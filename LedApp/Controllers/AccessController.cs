using LedApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LedApp.Controllers
{
    public class AccessController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;

        public AccessController(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Forbidden(string returnUrl)
        {
            // Chưa login → redirect Login
            if (!User.Identity!.IsAuthenticated)
                return Redirect($"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");

            // Đang login bằng tài khoản Admin → ở trong trang Admin bị denied
            // → hiện Access Denied của Identity, KHÔNG logout
            if (User.IsInRole(nameof(Quyen.Admin)))
                return Redirect($"/Identity/Account/AccessDenied?ReturnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");

            // Không phải Admin → đang ở Nav click sang trang khác
            // → logout → Login để đăng nhập đúng tài khoản
            await _signInManager.SignOutAsync();
            return Redirect($"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }
    }
}
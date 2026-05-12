using LedApp.Data;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    [Authorize(Policy = "Nav.View")]
    public class NavController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<AppUser> _userManager;

        public NavController(ApplicationDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var cuaNhaps = await _context.CuaNhaps
                .OrderBy(c => c.Ten)
                .ToListAsync();
            var cuaXuats = await _context.CuaXuats
                .OrderBy(c => c.Ten)
                .ToListAsync();
            ViewBag.CuaNhaps = cuaNhaps;
            ViewBag.CuaXuats = cuaXuats;

            // Lấy tất cả quyền của user
            var user = await _userManager.GetUserAsync(User);
            var claims = user != null
                ? await _userManager.GetClaimsAsync(user)
                : new List<System.Security.Claims.Claim>();

            // Tập hợp tất cả quyền (Extra + role)
            var userPerms = claims
                .Where(c => c.Type == "QuyenExtra" || c.Type == "Quyen")
                .Select(c => c.Value)
                .ToHashSet();

            // Admin có tất cả
            if (User.IsInRole("Admin"))
                userPerms.Add("*");

            ViewBag.UserPerms = userPerms;
            return View();
        }
    }
}
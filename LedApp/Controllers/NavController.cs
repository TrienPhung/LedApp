using LedApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    [Authorize]
    public class NavController : Controller
    {
        private readonly ApplicationDBContext _context;
        public NavController(ApplicationDBContext context)
        {
            _context = context;
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
            return View();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CuaXuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        public CuaXuatsController(ApplicationDBContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            _context.CuaXuats != null
                ? View(await _context.CuaXuats.ToListAsync())
                : Problem("Entity set 'ApplicationDBContext.CuaXuats' is null.");

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Ten,Mota,IsActive")] CuaXuat cuaXuat)
        {
            try
            {
                _context.Add(cuaXuat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch { return View(cuaXuat); }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,Mota,IsActive")] CuaXuat cuaXuat)
        {
            if (id != cuaXuat.Id) return NotFound();
            try
            {
                var existing = await _context.CuaXuats.AsTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (existing == null) return NotFound();

                existing.Ten = cuaXuat.Ten;
                existing.Mota = cuaXuat.Mota;
                existing.IsActive = cuaXuat.IsActive;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException) { return View(cuaXuat); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.CuaXuats.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CuaXuats == null)
                return Problem("Entity set 'ApplicationDBContext.CuaXuats' is null.");
            var item = await _context.CuaXuats.FindAsync(id);
            if (item != null) _context.CuaXuats.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CuaXuatExists(int id) =>
            (_context.CuaXuats?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
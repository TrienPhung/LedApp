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

        [Authorize(Policy = "CuaXuat.View")]
        public async Task<IActionResult> Index() =>
            _context.CuaXuats != null
                ? View(await _context.CuaXuats.ToListAsync())
                : Problem("Entity set 'ApplicationDBContext.CuaXuats' is null.");

        [Authorize(Policy = "CuaXuat.View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }
        [Authorize(Policy = "CuaXuat.Create")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Policy = "CuaXuat.Create")]
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

        [Authorize(Policy = "CuaXuat.Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [Authorize(Policy = "CuaXuat.Edit")]
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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var item = await _context.CuaXuats
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == id);  // ← đổi FindAsync thành này

            if (item == null) return NotFound();
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CuaXuat.Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaXuats == null) return NotFound();
            var item = await _context.CuaXuats.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "CuaXuat.Delete")]
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

        [HttpPost]
        [Authorize(Policy = "CuaXuat.Delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteCuaRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var items = _context.CuaXuats.Where(c => request.Ids.Contains(c.Id));
            _context.CuaXuats.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} cửa xuất." });
        }

      
        private bool CuaXuatExists(int id) =>
            (_context.CuaXuats?.Any(e => e.Id == id)).GetValueOrDefault();
    }
    public class BulkDeleteCuaRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
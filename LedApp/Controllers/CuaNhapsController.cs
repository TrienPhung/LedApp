using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CuaNhapsController : Controller
    {
        private readonly ApplicationDBContext _context;
        public CuaNhapsController(ApplicationDBContext context) => _context = context;

        [Authorize(Policy = "CuaNhap.View")]
        public async Task<IActionResult> Index() =>
            _context.CuaNhaps != null
                ? View(await _context.CuaNhaps.ToListAsync())
                : Problem("Entity set 'ApplicationDBContext.CuaNhaps' is null.");
        [Authorize(Policy = "CuaNhap.View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }
        [Authorize(Policy = "CuaNhap.Create")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Policy = "CuaNhap.Create")]
        public async Task<IActionResult> Create([Bind("Id,Ten,Mota,IsActive")] CuaNhap cuaNhap)
        {
            try
            {
                _context.Add(cuaNhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch { return View(cuaNhap); }
        }
        [Authorize(Policy = "CuaNhap.Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [Authorize(Policy = "CuaNhap.Edit")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,Mota,IsActive")] CuaNhap cuaNhap)
        {
            if (id != cuaNhap.Id) return NotFound();
            try
            {
                var existing = await _context.CuaNhaps.AsTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (existing == null) return NotFound();

                existing.Ten = cuaNhap.Ten;
                existing.Mota = cuaNhap.Mota;
                existing.IsActive = cuaNhap.IsActive;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException) { return View(cuaNhap); }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleActive(int id, string? returnUrl = null)
        {
            var item = await _context.CuaNhaps
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null) return NotFound();
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Policy = "CuaNhap.Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "CuaNhap.Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CuaNhaps == null)
                return Problem("Entity set 'ApplicationDBContext.CuaNhaps' is null.");
            var item = await _context.CuaNhaps.FindAsync(id);
            if (item != null) _context.CuaNhaps.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = "CuaNhap.Delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteCuaNhapRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var items = _context.CuaNhaps.Where(c => request.Ids.Contains(c.Id));
            _context.CuaNhaps.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} cửa nhập." });
        }

        private bool CuaNhapExists(int id) =>
            (_context.CuaNhaps?.Any(e => e.Id == id)).GetValueOrDefault();
    }

    public class BulkDeleteCuaNhapRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
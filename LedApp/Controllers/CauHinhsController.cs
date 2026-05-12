using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CauHinhsController : Controller
    {
        private readonly ApplicationDBContext _context;
        public CauHinhsController(ApplicationDBContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            _context.CauHinhs != null
                ? View(await _context.CauHinhs.ToListAsync())
                : Problem("Entity set 'ApplicationDBContext.CauHinhs' is null.");

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CauHinhs == null) return NotFound();
            var item = await _context.CauHinhs.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Key,Value")] CauHinh cauHinh)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cauHinh);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cauHinh);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CauHinhs == null) return NotFound();
            var item = await _context.CauHinhs.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Key,Value")] CauHinh cauHinh)
        {
            if (id != cauHinh.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.CauHinhs.AsTracking()
                        .FirstOrDefaultAsync(c => c.Id == id);
                    if (existing == null) return NotFound();

                    existing.Key = cauHinh.Key;
                    existing.Value = cauHinh.Value;

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CauHinhExists(cauHinh.Id)) return NotFound();
                    else throw;
                }
            }
            return View(cauHinh);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CauHinhs == null) return NotFound();
            var item = await _context.CauHinhs.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CauHinhs == null)
                return Problem("Entity set 'ApplicationDBContext.CauHinhs' is null.");
            var item = await _context.CauHinhs.FindAsync(id);
            if (item != null) _context.CauHinhs.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteCauHinhRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var items = _context.CauHinhs.Where(c => request.Ids.Contains(c.Id));
            _context.CauHinhs.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} cấu hình." });
        }

        private bool CauHinhExists(int id) =>
            (_context.CauHinhs?.Any(e => e.Id == id)).GetValueOrDefault();
    }

    public class BulkDeleteCauHinhRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CanhBaosController : Controller
    {
        private readonly ApplicationDBContext _context;
        public CanhBaosController(ApplicationDBContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            View(await _context.CanhBaos.ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.CanhBaos.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Bind("LoaiPhieu,PhieuId,LoaiCanhBao,ThoiGian,GhiChu,TrangThai")] CanhBao canhBao)
        {
            try
            {
                _context.Add(canhBao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch { return View(canhBao); }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.CanhBaos.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LoaiPhieu,PhieuId,LoaiCanhBao,ThoiGian,GhiChu,TrangThai")] CanhBao canhBao)
        {
            if (id != canhBao.Id) return NotFound();
            try
            {
                var existing = await _context.CanhBaos.FindAsync(id);
                if (existing == null) return NotFound();
                existing.LoaiPhieu = canhBao.LoaiPhieu;
                existing.PhieuId = canhBao.PhieuId;
                existing.LoaiCanhBao = canhBao.LoaiCanhBao;
                existing.ThoiGian = canhBao.ThoiGian;
                existing.GhiChu = canhBao.GhiChu;
                existing.TrangThai = canhBao.TrangThai;
                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CanhBaos.Any(e => e.Id == id)) return NotFound();
                return View(canhBao);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.CanhBaos.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.CanhBaos.FindAsync(id);
            if (item != null) _context.CanhBaos.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteCanhBaoRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var items = _context.CanhBaos.Where(c => request.Ids.Contains(c.Id));
            _context.CanhBaos.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} cảnh báo." });
        }

        private bool CanhBaoExists(int id) =>
            (_context.CanhBaos?.Any(e => e.Id == id)).GetValueOrDefault();
    }

    public class BulkDeleteCanhBaoRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
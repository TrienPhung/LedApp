using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class LichSuBanGiaosController : Controller
    {
        private readonly ApplicationDBContext _context;

        public LichSuBanGiaosController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var data = _context.LichSuBanGiaos.Include(l => l.NhanVien);
            return View(await data.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lichSu == null) return NotFound();
            return View(lichSu);
        }

        public IActionResult Create()
        {
            ViewData["NhanVienId"] = new SelectList(_context.nguoiDungs, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("LoaiPhieu,PhieuId,ChiTietDonViId,DonVi,SoBG,NhanVienId,ThoiGian")] LichSuBanGiao lichSu)
        {
            try
            {
                _context.Add(lichSu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ViewData["NhanVienId"] = new SelectList(_context.nguoiDungs, "Id", "Name", lichSu.NhanVienId);
                return View(lichSu);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos.FindAsync(id);
            if (lichSu == null) return NotFound();
            ViewData["NhanVienId"] = new SelectList(_context.nguoiDungs, "Id", "Name", lichSu.NhanVienId);
            return View(lichSu);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LoaiPhieu,PhieuId,ChiTietDonViId,DonVi,SoBG,NhanVienId,ThoiGian")] LichSuBanGiao lichSu)
        {
            if (id != lichSu.Id) return NotFound();
            try
            {
                var existing = await _context.LichSuBanGiaos.FindAsync(id);
                if (existing == null) return NotFound();
                existing.LoaiPhieu = lichSu.LoaiPhieu;
                existing.PhieuId = lichSu.PhieuId;
                existing.ChiTietDonViId = lichSu.ChiTietDonViId;
                existing.DonVi = lichSu.DonVi;
                existing.SoBG = lichSu.SoBG;
                existing.NhanVienId = lichSu.NhanVienId;
                existing.ThoiGian = lichSu.ThoiGian;
                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.LichSuBanGiaos.Any(e => e.Id == id)) return NotFound();
                ViewData["NhanVienId"] = new SelectList(_context.nguoiDungs, "Id", "Name", lichSu.NhanVienId);
                return View(lichSu);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lichSu == null) return NotFound();
            return View(lichSu);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichSu = await _context.LichSuBanGiaos.FindAsync(id);
            if (lichSu != null) _context.LichSuBanGiaos.Remove(lichSu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
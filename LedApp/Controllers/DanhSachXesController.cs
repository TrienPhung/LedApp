using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Data;
using Microsoft.AspNetCore.Authorization;

namespace LedApp.Controllers
{
    [Authorize]
    public class DanhSachXesController : Controller
    {
        private readonly ApplicationDBContext _context;

        public DanhSachXesController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: DanhSachXes
        public async Task<IActionResult> Index()
        {
            var danhSachXe = _context.DanhSachXes
                .Include(x => x.TaiXe);
            return View(await danhSachXe.ToListAsync());
        }

        // GET: DanhSachXes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (xe == null) return NotFound();
            return View(xe);
        }

        // GET: DanhSachXes/Create
        public IActionResult Create()
        {
            ViewData["TaiXeId"] = new SelectList(
                _context.nguoiDungs.Where(u => u.Quyen == (int)Quyen.TaiXe),
                "Id", "Name");
            return View();
        }

        // POST: DanhSachXes/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("Id,BienSoXe,LoaiXe,TaiTrong,TrangThai,GhiChu,ThoiGianDuKienVe,TaiXeId")] DanhSachXe xe)
        {
            try
            {
                _context.Add(xe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["TaiXeId"] = new SelectList(
                    _context.nguoiDungs.Where(u => u.Quyen == (int)Quyen.TaiXe),
                    "Id", "Name", xe.TaiXeId);
                return View(xe);
            }
        }

        // GET: DanhSachXes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var xe = await _context.DanhSachXes.FindAsync(id);
            if (xe == null) return NotFound();

            ViewData["TaiXeId"] = new SelectList(
                _context.nguoiDungs.Where(u => u.Quyen == (int)Quyen.TaiXe),
                "Id", "Name", xe.TaiXeId);
            return View(xe);
        }

        // POST: DanhSachXes/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,BienSoXe,LoaiXe,TaiTrong,TrangThai,GhiChu,ThoiGianDuKienVe,TaiXeId")] DanhSachXe xe)
        {
            if (id != xe.Id) return NotFound();

            // THÊM ĐOẠN NÀY ĐỂ DEBUG
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = string.Join(" | ", errors);
                ViewData["TaiXeId"] = new SelectList(
                    _context.nguoiDungs.Where(u => u.Quyen == (int)Quyen.TaiXe),
                    "Id", "Name", xe.TaiXeId);
                return View(xe);
            }

            try
            {
                _context.Update(xe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message; // THÊM để xem lỗi
                ViewData["TaiXeId"] = new SelectList(
                    _context.nguoiDungs.Where(u => u.Quyen == (int)Quyen.TaiXe),
                    "Id", "Name", xe.TaiXeId);
                return View(xe);
            }
        }

        // GET: DanhSachXes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (xe == null) return NotFound();
            return View(xe);
        }

        // POST: DanhSachXes/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var xe = await _context.DanhSachXes.FindAsync(id);
                if (xe != null)
                {
                    if (xe.TrangThai == (int)TrangThaiXe.DangPhanCong ||
                        xe.TrangThai == (int)TrangThaiXe.DangVanChuyen)
                    {
                        TempData["Error"] = "Không thể xóa xe đang hoạt động!";
                        return RedirectToAction(nameof(Index));
                    }
                    _context.DanhSachXes.Remove(xe);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        private bool DanhSachXeExists(int id)
        {
            return (_context.DanhSachXes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LedApp.Controllers
{
    [Authorize(Roles = "Admin,QuanLy")]
    public class DanhSachXesController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DanhSachXesController(ApplicationDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper: lấy danh sách nhân viên có role TaiXe
        private async Task<List<nguoiDungs>> GetDanhSachTaiXe()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(nameof(Quyen.TaiXe));
            var userIds = usersInRole.Select(u => u.Id).ToList();
            return await _context.nguoiDungs
                .Where(n => n.UserId != null && userIds.Contains(n.UserId))
                .ToListAsync();
        }

        public async Task<IActionResult> Index()
        {
            var danhSachXe = _context.DanhSachXes.Include(x => x.TaiXe);
            return View(await danhSachXe.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (xe == null) return NotFound();
            return View(xe);
        }

        public async Task<IActionResult> Create()
        {
            var taixe = await GetDanhSachTaiXe();
            ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName");
            return View();
        }

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
            catch
            {
                var taixe = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
                return View(xe);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes.FindAsync(id);
            if (xe == null) return NotFound();
            var taixe = await GetDanhSachTaiXe();
            ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
            return View(xe);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,BienSoXe,LoaiXe,TaiTrong,TrangThai,GhiChu,ThoiGianDuKienVe,TaiXeId")] DanhSachXe xe)
        {
            if (id != xe.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = string.Join(" | ", errors);
                var taixe = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
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
                TempData["Error"] = ex.Message;
                var taixe = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
                return View(xe);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (xe == null) return NotFound();
            return View(xe);
        }

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
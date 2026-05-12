using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;
using Microsoft.AspNetCore.Identity;

namespace LedApp.Controllers
{
    [Authorize]
    public class LichSuBanGiaosController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LichSuBanGiaosController(ApplicationDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Helper: chỉ lấy nhân viên có quyền NhanVien
        private async Task<List<nguoiDungs>> GetDanhSachNhanVien()
        {
            var userIds = (await _userManager.GetUsersInRoleAsync("NhanVien"))
                .Select(u => u.Id).ToHashSet();

            return _context.nguoiDungs
                .AsEnumerable()
                .Where(nv => nv.UserId != null && userIds.Contains(nv.UserId))
                .ToList();
        }

        // GET: LichSuBanGiaos
        public async Task<IActionResult> Index()
        {
            var data = _context.LichSuBanGiaos.Include(l => l.NhanVien);
            return View(await data.ToListAsync());
        }

        // GET: LichSuBanGiaos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lichSu == null) return NotFound();
            return View(lichSu);
        }

        // GET: LichSuBanGiaos/Create
        public async Task<IActionResult> Create()
        {
            ViewData["NhanVienId"] = new SelectList(await GetDanhSachNhanVien(), "Id", "FullName");
            return View();
        }

        // POST: LichSuBanGiaos/Create
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
                ViewData["NhanVienId"] = new SelectList(await GetDanhSachNhanVien(), "Id", "FullName", lichSu.NhanVienId);
                return View(lichSu);
            }
        }

        // GET: LichSuBanGiaos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos.FindAsync(id);
            if (lichSu == null) return NotFound();
            ViewData["NhanVienId"] = new SelectList(await GetDanhSachNhanVien(), "Id", "FullName", lichSu.NhanVienId);
            return View(lichSu);
        }

        // POST: LichSuBanGiaos/Edit/5
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
                ViewData["NhanVienId"] = new SelectList(await GetDanhSachNhanVien(), "Id", "FullName", lichSu.NhanVienId);
                return View(lichSu);
            }
        }

        // GET: LichSuBanGiaos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lichSu == null) return NotFound();
            return View(lichSu);
        }

        // POST: LichSuBanGiaos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichSu = await _context.LichSuBanGiaos.FindAsync(id);
            if (lichSu != null) _context.LichSuBanGiaos.Remove(lichSu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: LichSuBanGiaos/BulkDelete
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteLichSuRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var items = _context.LichSuBanGiaos.Where(l => request.Ids.Contains(l.Id));
            _context.LichSuBanGiaos.RemoveRange(items);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} lịch sử bàn giao." });
        }
    }

    public class BulkDeleteLichSuRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
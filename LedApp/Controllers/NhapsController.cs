using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class NhapsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public NhapsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // Helper dropdown TrangThai
        private SelectList GetTrangThaiSelectList(int? selectedValue = null)
        {
            var list = new List<object>
            {
                new { Value = (int)TrangThaiNhap.DaPhanCong,  Text = "Đã phân công" },
                new { Value = (int)TrangThaiNhap.DangBanGiao, Text = "Đang bàn giao" },
                new { Value = (int)TrangThaiNhap.QuaThoiGian, Text = "Quá thời gian" },
                new { Value = (int)TrangThaiNhap.HoanThanh,   Text = "Hoàn thành" }
            };
            return new SelectList(list, "Value", "Text", selectedValue);
        }

        // GET: Nhaps
        [Authorize(Policy = "Nhap.View")]
        public async Task<IActionResult> Index()
        {
            var applicationDBContext = _context.Nhaps
                .Include(n => n.CuaNhap)
                .Include(n => n.NhanVienXacNhan);
            return View(await applicationDBContext.ToListAsync());
        }

        // GET: Nhaps/Details/5
        [Authorize(Policy = "Nhap.View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Nhaps == null)
                return NotFound();

            var nhap = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .Include(n => n.NhanVienXacNhan)
                .Include(n => n.ChitietNhaps)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (nhap == null) return NotFound();
            return View(nhap);
        }

        // GET: Nhaps/Create
        [Authorize(Policy = "Nhap.Create")]
        public IActionResult Create()
        {
            ViewData["CuaNhapId"]          = new SelectList(_context.CuaNhaps, "Id", "Ten");
            ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "FullName");
            ViewData["TrangThai"]          = GetTrangThaiSelectList((int)TrangThaiNhap.DaPhanCong);
            return View();
        }

        // POST: Nhaps/Create
        [HttpPost]
        [Authorize(Policy = "Nhap.Create")]
        public async Task<IActionResult> Create([Bind("CuaNhapId,BienSoXe,ThoiGianPhanCong,ThoiGianVaoBai,ThoiGianVaoCua,ThoiGianGioiHan,ThoiGianHoanThanh,NhanVienXacNhanId,TrangThai,GhiChu")] Nhap nhap)
        {
            try
            {
                _context.Add(nhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ViewData["CuaNhapId"]         = new SelectList(_context.CuaNhaps, "Id", "Ten", nhap.CuaNhapId);
                ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name", nhap.NhanVienXacNhanId);
                ViewData["TrangThai"]         = GetTrangThaiSelectList(nhap.TrangThai);
                return View(nhap);
            }
        }

        // GET: Nhaps/Edit/5
        [Authorize(Policy = "Nhap.Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Nhaps == null) return NotFound();

            var nhap = await _context.Nhaps.FindAsync(id);
            if (nhap == null) return NotFound();

            ViewData["CuaNhapId"]         = new SelectList(_context.CuaNhaps, "Id", "Ten", nhap.CuaNhapId);
            ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "FullName", nhap.NhanVienXacNhanId);
            ViewData["TrangThai"]         = GetTrangThaiSelectList(nhap.TrangThai);
            return View(nhap);
        }

        // POST: Nhaps/Edit/5
        [HttpPost]
        [Authorize(Policy = "Nhap.Edit")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CuaNhapId,BienSoXe,ThoiGianPhanCong,ThoiGianVaoBai,ThoiGianVaoCua,ThoiGianGioiHan,ThoiGianHoanThanh,NhanVienXacNhanId,TrangThai,GhiChu")] Nhap nhap)
        {
            if (id != nhap.Id) return NotFound();

            try
            {
                var existing = await _context.Nhaps.FindAsync(id);
                if (existing == null) return NotFound();

                existing.CuaNhapId         = nhap.CuaNhapId;
                existing.BienSoXe          = nhap.BienSoXe;
                existing.ThoiGianPhanCong  = nhap.ThoiGianPhanCong;
                existing.ThoiGianVaoBai    = nhap.ThoiGianVaoBai;
                existing.ThoiGianVaoCua    = nhap.ThoiGianVaoCua;
                existing.ThoiGianGioiHan   = nhap.ThoiGianGioiHan;
                existing.ThoiGianHoanThanh = nhap.ThoiGianHoanThanh;
                existing.NhanVienXacNhanId = nhap.NhanVienXacNhanId;
                existing.TrangThai         = nhap.TrangThai;
                existing.GhiChu = nhap.GhiChu;

                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NhapExists(id)) return NotFound();

                ViewData["CuaNhapId"]         = new SelectList(_context.CuaNhaps, "Id", "Ten", nhap.CuaNhapId);
                ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name", nhap.NhanVienXacNhanId);
                ViewData["TrangThai"]         = GetTrangThaiSelectList(nhap.TrangThai);
                return View(nhap);
            }
        }

        // GET: Nhaps/Delete/5
        [Authorize(Policy = "Nhap.Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Nhaps == null) return NotFound();

            var nhap = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .Include(n => n.NhanVienXacNhan)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (nhap == null) return NotFound();
            return View(nhap);
        }

        // POST: Nhaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Nhap.Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhap = await _context.Nhaps
                .Include(n => n.ChitietNhaps) // ← THÊM: load chitiet theo
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nhap != null)
            {
                // Xóa chitiet trước
                _context.ChitietNhaps.RemoveRange(nhap.ChitietNhaps);
                // Rồi mới xóa phiếu nhập
                _context.Nhaps.Remove(nhap);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [Authorize(Policy = "Nhap.Delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteNhapRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            foreach (var id in request.Ids)
            {
                var nhap = await _context.Nhaps
                    .Include(n => n.ChitietNhaps)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (nhap == null) continue;

                _context.ChitietNhaps.RemoveRange(nhap.ChitietNhaps ?? new List<ChitietNhap>());
                _context.Nhaps.Remove(nhap);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa {request.Ids.Count} phiếu nhập." });
        }
        private bool NhapExists(int id)
        {
            return (_context.Nhaps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
    public class BulkDeleteNhapRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
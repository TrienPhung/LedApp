using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class nguoiDungsController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IWebHostEnvironment _env;

        public nguoiDungsController(ApplicationDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var list = await _context.nguoiDungs
                .Include(n => n.User)
                .ToListAsync();
            return View(list);
        }

        // ===== DETAILS =====
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var nguoi = await _context.nguoiDungs
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        // ===== CREATE GET =====
        public IActionResult Create() => View();

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("LastName,FirstName,NgaySinh,SoDienThoai,DiaChi,GioiTinh")] nguoiDungs nguoi,
            IFormFile? avatarFile)
        {
            ModelState.Remove("UserId");
            if (!ModelState.IsValid) return View(nguoi);

            nguoi.Image = await SaveAvatarAsync(avatarFile) ?? "/uploads/avatars/default.png";

            _context.Add(nguoi);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT GET =====
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nguoi = await _context.nguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        // ===== EDIT POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,LastName,FirstName,NgaySinh,SoDienThoai,DiaChi,GioiTinh,Image")] nguoiDungs nguoi,
            IFormFile? avatarFile)
        {
            if (id != nguoi.Id) return NotFound();
            ModelState.Remove("UserId");
            ModelState.Remove("Image");
            if (!ModelState.IsValid) return View(nguoi);

            var existing = await _context.nguoiDungs.FindAsync(id);
            if (existing == null) return NotFound();

            existing.LastName = nguoi.LastName;
            existing.FirstName = nguoi.FirstName;
            existing.NgaySinh = nguoi.NgaySinh;
            existing.SoDienThoai = nguoi.SoDienThoai;
            existing.DiaChi = nguoi.DiaChi;
            existing.GioiTinh = nguoi.GioiTinh;

            if (avatarFile != null && avatarFile.Length > 0)
            {
                DeleteOldAvatar(existing.Image);
                existing.Image = await SaveAvatarAsync(avatarFile);
            }

            _context.Entry(existing).State = EntityState.Modified;
            _context.Entry(existing).Property(x => x.UserId).IsModified = false;
            if (avatarFile == null || avatarFile.Length == 0)
                _context.Entry(existing).Property(x => x.Image).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.nguoiDungs.Any(e => e.Id == id)) return NotFound();
                throw;
            }
        }

        // ===== DELETE GET =====
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var nguoi = await _context.nguoiDungs
                .Include(n => n.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        // ===== DELETE POST =====
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoi = await _context.nguoiDungs.FindAsync(id);
            if (nguoi == null) return NotFound();

            bool dangDungNhap = await _context.Nhaps.AnyAsync(n => n.NhanVienXacNhanId == id);
            bool dangDungXuat = await _context.Xuats.AnyAsync(x => x.NhanVienXacNhanId == id);

            if (dangDungNhap || dangDungXuat)
            {
                TempData["Error"] = "Không thể xóa — nhân viên này đang có phiếu nhập/xuất liên quan!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(nguoi.UserId))
            {
                TempData["Error"] = "Không thể xóa — nhân viên này đang có tài khoản. Hãy xóa tài khoản trước!";
                return RedirectToAction(nameof(Index));
            }

            DeleteOldAvatar(nguoi.Image);
            _context.nguoiDungs.Remove(nguoi);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===== BULK DELETE =====
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteNguoiDungRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            var errors = new List<string>();
            var toDelete = new List<nguoiDungs>();

            foreach (var id in request.Ids)
            {
                var nguoi = await _context.nguoiDungs.FindAsync(id);
                if (nguoi == null) continue;

                bool dangDungNhap = await _context.Nhaps.AnyAsync(n => n.NhanVienXacNhanId == id);
                bool dangDungXuat = await _context.Xuats.AnyAsync(x => x.NhanVienXacNhanId == id);

                if (dangDungNhap || dangDungXuat || !string.IsNullOrEmpty(nguoi.UserId))
                {
                    errors.Add(nguoi.FirstName + " " + nguoi.LastName);
                    continue;
                }
                toDelete.Add(nguoi);
            }

            foreach (var n in toDelete) DeleteOldAvatar(n.Image);
            _context.nguoiDungs.RemoveRange(toDelete);
            await _context.SaveChangesAsync();

            if (errors.Any())
                return Ok(new { message = $"Đã xóa {toDelete.Count} nhân viên. Bỏ qua {errors.Count} do có ràng buộc." });

            return Ok(new { message = $"Đã xóa {toDelete.Count} nhân viên." });
        }

        // ===== HELPERS =====
        private async Task<string?> SaveAvatarAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(folder, fileName);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/avatars/{fileName}";
        }

        private void DeleteOldAvatar(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            if (imagePath.Contains("default.png")) return;
            var oldPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }
    }

    public class BulkDeleteNguoiDungRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
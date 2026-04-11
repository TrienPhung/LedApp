using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class nguoiDungsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public nguoiDungsController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.nguoiDungs.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var nguoi = await _context.nguoiDungs.FirstOrDefaultAsync(m => m.Id == id);
            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        public IActionResult Create() => View();
        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("Username,Password,Name,Tels,Email,Quyen")] nguoiDungs nguoi,
            IFormFile? avatarFile)
        {
            if (!ModelState.IsValid) return View(nguoi);

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatarFile.FileName)}";
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await avatarFile.CopyToAsync(stream);
                nguoi.Image = $"/uploads/avatars/{fileName}";
            }
            else
            {
                nguoi.Image = "/uploads/avatars/default.png"; // ← thêm dòng này
            }

            _context.Add(nguoi);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var nguoi = await _context.nguoiDungs
                .AsNoTracking() // ← thêm
                .FirstOrDefaultAsync(x => x.Id == id);
            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Username,Password,Name,Tels,Email,Quyen,Image")] nguoiDungs nguoi,
            IFormFile? avatarFile)
        {
            if (id != nguoi.Id) return NotFound();
            ModelState.Remove("Password");
            if (!ModelState.IsValid) return View(nguoi);

            var existing = await _context.nguoiDungs.FindAsync(id);
            if (existing == null) return NotFound();

            try
            {
                existing.Username = nguoi.Username;
                if (!string.IsNullOrWhiteSpace(nguoi.Password))
                    existing.Password = nguoi.Password;
                existing.Name = nguoi.Name;
                existing.Tels = nguoi.Tels;
                existing.Email = nguoi.Email;
                existing.Quyen = nguoi.Quyen;

                _context.Entry(existing).State = EntityState.Modified; // ← thêm

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.Image))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existing.Image.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatarFile.FileName)}";
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");
                    Directory.CreateDirectory(folder);
                    var path = Path.Combine(folder, fileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    await avatarFile.CopyToAsync(stream);
                    existing.Image = $"/uploads/avatars/{fileName}";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.nguoiDungs.Any(e => e.Id == id)) return NotFound();
                return View(nguoi);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var nguoi = await _context.nguoiDungs.FirstOrDefaultAsync(m => m.Id == id);
            if (nguoi == null) return NotFound();
            return View(nguoi);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoi = await _context.nguoiDungs.FindAsync(id);
            if (nguoi == null) return NotFound();

            // Kiểm tra FK trước khi xóa
            bool dangDungNhap = await _context.Nhaps.AnyAsync(n => n.NhanVienXacNhanId == id);
            bool dangDungXuat = await _context.Xuats.AnyAsync(x => x.NhanVienXacNhanId == id);

            if (dangDungNhap || dangDungXuat)
            {
                TempData["Error"] = "Không thể xóa — nhân viên này đang có phiếu nhập/xuất liên quan!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(nguoi.Image))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", nguoi.Image.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            _context.nguoiDungs.Remove(nguoi);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
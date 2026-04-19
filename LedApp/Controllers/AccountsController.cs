using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;
using System.ComponentModel.DataAnnotations;

namespace LedApp.Controllers
{
    [Authorize(Roles = nameof(Quyen.Admin))]
    public class AccountsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDBContext _context;

        public AccountsController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDBContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // Lấy từ enum — tự động cập nhật nếu thêm role mới
        private static List<string> GetAllRoles() =>
            Enum.GetNames<Quyen>().ToList();

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Include(u => u.NguoiDung)
                .ToListAsync();

            var model = new List<AccountIndexViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                model.Add(new AccountIndexViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    IsLocked = await _userManager.IsLockedOutAsync(u),
                    Roles = roles.ToList(),
                    NguoiDung = u.NguoiDung
                });
            }

            return View(model);
        }

        // ===== CREATE GET =====
        public async Task<IActionResult> Create(int? nguoiDungId = null)
        {
            var chuaCoTaiKhoan = await _context.nguoiDungs
                .Where(n => n.UserId == null)
                .ToListAsync();

            if (!chuaCoTaiKhoan.Any())
            {
                TempData["Info"] = "Tất cả nhân viên đã có tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            return View(new AccountCreateViewModel
            {
                DanhSachNhanVien = chuaCoTaiKhoan,
                AvailableRoles = GetAllRoles(),
                Role = nameof(Quyen.NhanVien),
                NguoiDungId = nguoiDungId ?? 0  // ← tự chọn sẵn nhân viên nếu có
            });
        }

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel vm)
        {
            vm.DanhSachNhanVien = await _context.nguoiDungs
                .Where(n => n.UserId == null)
                .ToListAsync();
            vm.AvailableRoles = GetAllRoles();

            if (!Enum.TryParse<Quyen>(vm.Role, out _))
                ModelState.AddModelError(nameof(vm.Role), "Quyền không hợp lệ.");

            if (!ModelState.IsValid) return View(vm);

            var nguoi = await _context.nguoiDungs
                .AsTracking()  // ← THÊM để EF theo dõi thay đổi
                .FirstOrDefaultAsync(n => n.Id == vm.NguoiDungId);

            if (nguoi == null)
            {
                ModelState.AddModelError("", "Nhân viên không tồn tại.");
                return View(vm);
            }

            if (nguoi.UserId != null)
            {
                ModelState.AddModelError("", "Nhân viên này đã có tài khoản.");
                return View(vm);
            }

            var user = new AppUser
            {
                UserName = vm.UserName,
                Email = vm.Email,
                EmailConfirmed = true  // ← thêm dòng này
            };
            var result = await _userManager.CreateAsync(user, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, vm.Role!);

            nguoi.UserId = user.Id;
            await _context.SaveChangesAsync();  // ← giờ sẽ lưu được vì có AsTracking

            TempData["Success"] = $"Tạo tài khoản '{vm.UserName}' với quyền '{vm.Role}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT GET =====
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.Users
                .Include(u => u.NguoiDung)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);

            return View(new AccountEditViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = currentRoles.FirstOrDefault() ?? nameof(Quyen.NhanVien),
                AvailableRoles = GetAllRoles(),
                NguoiDung = user.NguoiDung
            });
        }

        // ===== EDIT POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AccountEditViewModel vm)
        {
            vm.AvailableRoles = GetAllRoles();

            if (!Enum.TryParse<Quyen>(vm.Role, out _))
                ModelState.AddModelError(nameof(vm.Role), "Quyền không hợp lệ.");

            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            user.UserName = vm.UserName;
            user.Email = vm.Email;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Đổi password nếu có nhập
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

                if (!pwResult.Succeeded)
                {
                    foreach (var e in pwResult.Errors)
                        ModelState.AddModelError("", e.Description);
                    return View(vm);
                }
            }

            // Xóa role cũ → gán role mới — không cần check tồn tại vì đã seed
            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, vm.Role!);

            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ===== TOGGLE LOCK =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["Success"] = $"Đã mở khóa tài khoản '{user.UserName}'.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["Success"] = $"Đã khóa tài khoản '{user.UserName}'.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE GET =====
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.Users
                .Include(u => u.NguoiDung)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            ViewBag.Roles = await _userManager.GetRolesAsync(user);
            return View(user);
        }

        // ===== DELETE POST =====
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.NguoiDung)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            // Dùng thẳng navigation property, KHÔNG query lại
            if (user.NguoiDung != null)
            {
                user.NguoiDung.UserId = null;
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Xóa thất bại: " +
                    string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã xóa tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ===== VIEW MODELS =====

    public class AccountIndexViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsLocked { get; set; }
        public List<string> Roles { get; set; } = new();
        public nguoiDungs? NguoiDung { get; set; }
    }

    public class AccountCreateViewModel
    {
        [Required(ErrorMessage = "Chọn nhân viên")]
        public int NguoiDungId { get; set; }

        [Required(ErrorMessage = "Nhập username")]
        [MaxLength(256)]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Chọn quyền")]
        public string? Role { get; set; }

        public List<nguoiDungs> DanhSachNhanVien { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
    }

    public class AccountEditViewModel
    {
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "Nhập username")]
        [MaxLength(256)]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Nhập email")]
        [EmailAddress]
        public string Email { get; set; } = "";

        [MinLength(6, ErrorMessage = "Tối thiểu 6 ký tự")]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Chọn quyền")]
        public string? Role { get; set; }

        public List<string> AvailableRoles { get; set; } = new();
        public nguoiDungs? NguoiDung { get; set; }
    }
}
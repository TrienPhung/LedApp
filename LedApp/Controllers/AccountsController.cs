using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LedApp.Controllers
{
    [Authorize(Roles = "Admin")]
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

        // ── Quyền cho Role (IsExtraOnly = false) ──
        private async Task<List<PermissionGroup>> GetRolePermissionGroupsAsync()
        {
            var perms = await _context.Permissions
                .Where(p => p.IsActive && !p.IsExtraOnly)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Order)
                .ToListAsync();

            return perms
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroup(
                    g.Key,
                    g.Select(p => new PermissionItem(p.Value, p.Label)).ToList()
                ))
                .ToList();
        }

        // ── Quyền Extra cá nhân (IsExtraOnly = true) ──
        private async Task<List<PermissionGroup>> GetExtraPermissionGroupsAsync()
        {
            var perms = await _context.Permissions
                .Where(p => p.IsActive && p.IsExtraOnly)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Order)
                .ToListAsync();

            return perms
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroup(
                    g.Key,
                    g.Select(p => new PermissionItem(p.Value, p.Label)).ToList()
                ))
                .ToList();
        }

        // ── Lấy DenyGroups từ danh sách quyền của role ──
        private async Task<List<PermissionGroup>> GetDenyGroupsFromRoleAsync(IEnumerable<string> rolePermValues)
        {
            var valueSet = rolePermValues.ToHashSet();
            var perms = await _context.Permissions
                .Where(p => p.IsActive && !p.IsExtraOnly && valueSet.Contains(p.Value))
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Order)
                .ToListAsync();

            return perms
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroup(
                    g.Key,
                    g.Select(p => new PermissionItem(p.Value, p.Label)).ToList()
                ))
                .ToList();
        }

        // ── Đọc role từ Identity ──
        private async Task<List<string>> GetAllRolesAsync() =>
            await _roleManager.Roles
                .Select(r => r.Name!)
                .OrderBy(r => r)
                .ToListAsync();

        // ── Lấy quyền của role ──
        private async Task<HashSet<string>> GetRolePermissionValuesAsync(IList<string> roles)
        {
            var result = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;
                var rc = await _roleManager.GetClaimsAsync(role);
                foreach (var c in rc.Where(c => c.Type == "Quyen"))
                    result.Add(c.Value);
            }
            return result;
        }

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
                var claims = await _userManager.GetClaimsAsync(u);
                model.Add(new AccountIndexViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    IsLocked = await _userManager.IsLockedOutAsync(u),
                    Roles = roles.ToList(),
                    NguoiDung = u.NguoiDung,
                    ClaimCount = claims.Count(c =>
                        c.Type == "QuyenExtra" ||
                        c.Type == "QuyenDeny" ||
                        c.Type == "CuaXuat" ||
                        c.Type == "CuaNhap")
                });
            }
            return View(model);
        }

        // ===== DETAILS =====
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.Users
                .Include(u => u.NguoiDung)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            var cuaXuats = await _context.CuaXuats.ToListAsync();
            var cuaNhaps = await _context.CuaNhaps.ToListAsync();

            var allowedCuaXuatIds = claims
                .Where(c => c.Type == "CuaXuat")
                .Select(c => int.TryParse(c.Value, out var i) ? i : 0)
                .ToHashSet();
            var allowedCuaNhapIds = claims
                .Where(c => c.Type == "CuaNhap")
                .Select(c => int.TryParse(c.Value, out var i) ? i : 0)
                .ToHashSet();

            var rolePermissions = await GetRolePermissionValuesAsync(roles);

            // AllPermissions cho Details: tất cả quyền không ExtraOnly
            var allPerms = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.GroupName).ThenBy(p => p.Order)
                .ToListAsync();

            var allPermGroups = allPerms
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroup(
                    g.Key,
                    g.Select(p => new PermissionItem(p.Value, p.Label)).ToList()
                ))
                .ToList();

            return View(new AccountDetailsViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                IsLocked = await _userManager.IsLockedOutAsync(user),
                Roles = roles.ToList(),
                NguoiDung = user.NguoiDung,
                RolePermissions = rolePermissions.ToList(),
                ExtraPermissions = claims.Where(c => c.Type == "QuyenExtra").Select(c => c.Value).ToList(),
                DenyPermissions = claims.Where(c => c.Type == "QuyenDeny").Select(c => c.Value).ToList(),
                AllowedCuaXuats = cuaXuats.Where(c => allowedCuaXuatIds.Contains(c.Id)).ToList(),
                AllowedCuaNhaps = cuaNhaps.Where(c => allowedCuaNhapIds.Contains(c.Id)).ToList(),
                AllPermissions = allPermGroups,
            });
        }

        // ===== CREATE GET =====
        public async Task<IActionResult> Create(int? nguoiDungId = null)
        {
            var chuaCoTaiKhoan = await _context.nguoiDungs
                .Where(n => n.UserId == null).ToListAsync();

            if (!chuaCoTaiKhoan.Any())
            {
                TempData["Info"] = "Tất cả nhân viên đã có tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            var cuaXuats = await _context.CuaXuats.Where(c => c.IsActive).ToListAsync();
            var cuaNhaps = await _context.CuaNhaps.Where(c => c.IsActive).ToListAsync();

            return View(new AccountCreateViewModel
            {
                DanhSachNhanVien = chuaCoTaiKhoan,
                AvailableRoles = await GetAllRolesAsync(),
                ExtraPermissionGroups = await GetExtraPermissionGroupsAsync(),
                DenyPermissionGroups = new List<PermissionGroup>(), // rỗng vì chưa chọn role
                CuaXuats = cuaXuats,
                CuaNhaps = cuaNhaps,
                NguoiDungId = nguoiDungId ?? 0
            });
        }

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel vm)
        {
            vm.DanhSachNhanVien = await _context.nguoiDungs.Where(n => n.UserId == null).ToListAsync();
            vm.AvailableRoles = await GetAllRolesAsync();
            vm.ExtraPermissionGroups = await GetExtraPermissionGroupsAsync();
            vm.CuaXuats = await _context.CuaXuats.Where(c => c.IsActive).ToListAsync();
            vm.CuaNhaps = await _context.CuaNhaps.Where(c => c.IsActive).ToListAsync();

            if (string.IsNullOrWhiteSpace(vm.Role) || !await _roleManager.RoleExistsAsync(vm.Role))
                ModelState.AddModelError(nameof(vm.Role), "Role không hợp lệ.");

            if (!ModelState.IsValid)
            {
                // Load DenyGroups theo role đã chọn nếu có
                if (!string.IsNullOrWhiteSpace(vm.Role))
                {
                    var role = await _roleManager.FindByNameAsync(vm.Role);
                    if (role != null)
                    {
                        var rc = await _roleManager.GetClaimsAsync(role);
                        var vals = rc.Where(c => c.Type == "Quyen").Select(c => c.Value);
                        vm.DenyPermissionGroups = await GetDenyGroupsFromRoleAsync(vals);
                    }
                }
                else vm.DenyPermissionGroups = new List<PermissionGroup>();
                return View(vm);
            }
            // Kiểm tra trùng email
            var existingByEmail = await _userManager.FindByEmailAsync(vm.Email);
            if (existingByEmail != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng bởi tài khoản khác.");
                if (!string.IsNullOrWhiteSpace(vm.Role))
                {
                    var role = await _roleManager.FindByNameAsync(vm.Role);
                    if (role != null)
                    {
                        var rc = await _roleManager.GetClaimsAsync(role);
                        var vals = rc.Where(c => c.Type == "Quyen").Select(c => c.Value);
                        vm.DenyPermissionGroups = await GetDenyGroupsFromRoleAsync(vals);
                    }
                }
                else vm.DenyPermissionGroups = new List<PermissionGroup>();
                return View(vm);
            }
            var nguoi = await _context.nguoiDungs.AsTracking()
                .FirstOrDefaultAsync(n => n.Id == vm.NguoiDungId);

            if (nguoi == null) { ModelState.AddModelError("", "Nhân viên không tồn tại."); return View(vm); }
            if (nguoi.UserId != null) { ModelState.AddModelError("", "Nhân viên này đã có tài khoản."); return View(vm); }

            var user = new AppUser { UserName = vm.UserName, Email = vm.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, vm.Role!);

            if (vm.SelectedExtraPermissions != null)
                foreach (var p in vm.SelectedExtraPermissions)
                    await _userManager.AddClaimAsync(user, new Claim("QuyenExtra", p));

            if (vm.SelectedDenyPermissions != null)
                foreach (var p in vm.SelectedDenyPermissions)
                    await _userManager.AddClaimAsync(user, new Claim("QuyenDeny", p));

            if (vm.SelectedCuaXuatIds != null)
                foreach (var cid in vm.SelectedCuaXuatIds)
                    await _userManager.AddClaimAsync(user, new Claim("CuaXuat", cid.ToString()));

            if (vm.SelectedCuaNhapIds != null)
                foreach (var cid in vm.SelectedCuaNhapIds)
                    await _userManager.AddClaimAsync(user, new Claim("CuaNhap", cid.ToString()));

            nguoi.UserId = user.Id;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tạo tài khoản '{vm.UserName}' thành công!";
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
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var cuaXuats = await _context.CuaXuats.ToListAsync();
            var cuaNhaps = await _context.CuaNhaps.ToListAsync();

            var rolePermissions = await GetRolePermissionValuesAsync(currentRoles);

            // DenyGroups chỉ load quyền mà role hiện tại đang có
            var denyGroups = await GetDenyGroupsFromRoleAsync(rolePermissions);

            return View(new AccountEditViewModel
            {
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = currentRoles.FirstOrDefault() ?? "",
                AvailableRoles = await GetAllRolesAsync(),
                ExtraPermissionGroups = await GetExtraPermissionGroupsAsync(),
                DenyPermissionGroups = denyGroups,
                CuaXuats = cuaXuats,
                CuaNhaps = cuaNhaps,
                NguoiDung = user.NguoiDung,
                RolePermissions = rolePermissions.ToList(),
                SelectedExtraPermissions = currentClaims.Where(c => c.Type == "QuyenExtra").Select(c => c.Value).ToList(),
                SelectedDenyPermissions = currentClaims.Where(c => c.Type == "QuyenDeny").Select(c => c.Value).ToList(),
                SelectedCuaXuatIds = currentClaims.Where(c => c.Type == "CuaXuat").Select(c => int.Parse(c.Value)).ToList(),
                SelectedCuaNhapIds = currentClaims.Where(c => c.Type == "CuaNhap").Select(c => int.Parse(c.Value)).ToList(),
            });
        }

        // ===== EDIT POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AccountEditViewModel vm)
        {
            vm.AvailableRoles = await GetAllRolesAsync();
            vm.ExtraPermissionGroups = await GetExtraPermissionGroupsAsync();
            vm.CuaXuats = await _context.CuaXuats.ToListAsync();
            vm.CuaNhaps = await _context.CuaNhaps.ToListAsync();

            if (string.IsNullOrWhiteSpace(vm.Role) || !await _roleManager.RoleExistsAsync(vm.Role))
                ModelState.AddModelError(nameof(vm.Role), "Role không hợp lệ.");

            if (!ModelState.IsValid)
            {
                if (!string.IsNullOrWhiteSpace(vm.Role))
                {
                    var role = await _roleManager.FindByNameAsync(vm.Role);
                    if (role != null)
                    {
                        var rc = await _roleManager.GetClaimsAsync(role);
                        var vals = rc.Where(c => c.Type == "Quyen").Select(c => c.Value);
                        vm.DenyPermissionGroups = await GetDenyGroupsFromRoleAsync(vals);
                    }
                }
                else vm.DenyPermissionGroups = new List<PermissionGroup>();
                return View(vm);
            }
            // Kiểm tra trùng email (bỏ qua chính user đang sửa)
            var existingByEmail = await _userManager.FindByEmailAsync(vm.Email);
            if (existingByEmail != null && existingByEmail.Id != vm.UserId)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng bởi tài khoản khác.");
                if (!string.IsNullOrWhiteSpace(vm.Role))
                {
                    var role = await _roleManager.FindByNameAsync(vm.Role);
                    if (role != null)
                    {
                        var rc = await _roleManager.GetClaimsAsync(role);
                        var vals = rc.Where(c => c.Type == "Quyen").Select(c => c.Value);
                        vm.DenyPermissionGroups = await GetDenyGroupsFromRoleAsync(vals);
                    }
                }
                else vm.DenyPermissionGroups = new List<PermissionGroup>();
                return View(vm);
            }
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null) return NotFound();

            user.UserName = vm.UserName;
            user.Email = vm.Email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
                if (!pwResult.Succeeded)
                {
                    foreach (var e in pwResult.Errors) ModelState.AddModelError("", e.Description);
                    return View(vm);
                }
            }

            var oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, vm.Role!);

            var oldClaims = (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == "QuyenExtra" ||
                            c.Type == "QuyenDeny" ||
                            c.Type == "CuaXuat" ||
                            c.Type == "CuaNhap")
                .ToList();
            foreach (var c in oldClaims)
                await _userManager.RemoveClaimAsync(user, c);

            if (vm.SelectedExtraPermissions != null)
                foreach (var p in vm.SelectedExtraPermissions)
                    await _userManager.AddClaimAsync(user, new Claim("QuyenExtra", p));

            if (vm.SelectedDenyPermissions != null)
                foreach (var p in vm.SelectedDenyPermissions)
                    await _userManager.AddClaimAsync(user, new Claim("QuyenDeny", p));

            if (vm.SelectedCuaXuatIds != null)
                foreach (var cid in vm.SelectedCuaXuatIds)
                    await _userManager.AddClaimAsync(user, new Claim("CuaXuat", cid.ToString()));

            if (vm.SelectedCuaNhapIds != null)
                foreach (var cid in vm.SelectedCuaNhapIds)
                    await _userManager.AddClaimAsync(user, new Claim("CuaNhap", cid.ToString()));

            await _userManager.UpdateSecurityStampAsync(user);

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
            await _userManager.UpdateSecurityStampAsync(user);
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

            if (user.NguoiDung != null)
            {
                user.NguoiDung.UserId = null;
                await _context.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Xóa thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Đã xóa tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ===== PERMISSION MODELS =====
    public record PermissionItem(string Value, string Label);
    public record PermissionGroup(string GroupName, List<PermissionItem> Items);

    // ===== VIEW MODELS =====
    public class AccountIndexViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsLocked { get; set; }
        public int ClaimCount { get; set; }
        public List<string> Roles { get; set; } = new();
        public nguoiDungs? NguoiDung { get; set; }
    }

    public class AccountDetailsViewModel
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsLocked { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> RolePermissions { get; set; } = new();
        public List<string> ExtraPermissions { get; set; } = new();
        public List<string> DenyPermissions { get; set; } = new();
        public List<CuaXuat> AllowedCuaXuats { get; set; } = new();
        public List<CuaNhap> AllowedCuaNhaps { get; set; } = new();
        public List<PermissionGroup> AllPermissions { get; set; } = new();
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

        [Required(ErrorMessage = "Chọn role")]
        public string? Role { get; set; }

        public List<string>? SelectedExtraPermissions { get; set; }
        public List<string>? SelectedDenyPermissions { get; set; }
        public List<int>? SelectedCuaXuatIds { get; set; }
        public List<int>? SelectedCuaNhapIds { get; set; }

        public List<nguoiDungs> DanhSachNhanVien { get; set; } = new();
        public List<string> AvailableRoles { get; set; } = new();
        public List<PermissionGroup> ExtraPermissionGroups { get; set; } = new(); // tab Extra
        public List<PermissionGroup> DenyPermissionGroups { get; set; } = new();  // tab Deny
        public List<CuaXuat> CuaXuats { get; set; } = new();
        public List<CuaNhap> CuaNhaps { get; set; } = new();
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

        [Required(ErrorMessage = "Chọn role")]
        public string? Role { get; set; }

        public List<string>? SelectedExtraPermissions { get; set; }
        public List<string>? SelectedDenyPermissions { get; set; }
        public List<int>? SelectedCuaXuatIds { get; set; }
        public List<int>? SelectedCuaNhapIds { get; set; }

        public List<string> AvailableRoles { get; set; } = new();
        public List<PermissionGroup> ExtraPermissionGroups { get; set; } = new(); // tab Extra
        public List<PermissionGroup> DenyPermissionGroups { get; set; } = new();  // tab Deny
        public List<string> RolePermissions { get; set; } = new();
        public List<CuaXuat> CuaXuats { get; set; } = new();
        public List<CuaNhap> CuaNhaps { get; set; } = new();
        public nguoiDungs? NguoiDung { get; set; }
    }
}
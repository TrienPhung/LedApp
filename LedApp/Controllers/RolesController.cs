using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LedApp.Data;
using LedApp.Models;

namespace LedApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDBContext _context;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager,
            ApplicationDBContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        // ===== INDEX: danh sách role =====
        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var model = new List<RoleIndexViewModel>();
            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                var userCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
                model.Add(new RoleIndexViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name!,
                    PermissionCount = claims.Count(c => c.Type == "Quyen"),
                    UserCount = userCount
                });
            }

            return View(model);
        }

        // ===== CREATE GET =====
        public IActionResult Create() => View();

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Tên role không được để trống.");
                return View();
            }

            if (await _roleManager.RoleExistsAsync(roleName.Trim()))
            {
                ModelState.AddModelError("", $"Role '{roleName}' đã tồn tại.");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName.Trim()));
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError("", e.Description);
                return View();
            }

            TempData["Success"] = $"Tạo role '{roleName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            // Không cho xóa role Admin
            if (role.Name == "Admin")
            {
                TempData["Error"] = "Không thể xóa role Admin.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra còn user không
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (users.Any())
            {
                TempData["Error"] = $"Không thể xóa — role '{role.Name}' đang có {users.Count} tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Xóa thất bại: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Đã xóa role '{role.Name}'.";
            return RedirectToAction(nameof(Index));
        }

        // ===== PERMISSIONS GET: gán quyền cho role =====
        public async Task<IActionResult> Permissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            // Danh mục quyền từ bảng Permissions
            var allPerms = await _context.Permissions
                .Where(p => p.IsActive && !p.IsExtraOnly)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Order)
                .ToListAsync();

            // Quyền hiện tại của role từ AspNetRoleClaims
            var currentClaims = await _roleManager.GetClaimsAsync(role);
            var currentPerms = currentClaims
                .Where(c => c.Type == "Quyen")
                .Select(c => c.Value)
                .ToHashSet();

            // Group theo GroupName
            var grouped = allPerms
                .GroupBy(p => p.GroupName)
                .Select(g => new PermissionGroupViewModel
                {
                    GroupName = g.Key,
                    Items = g.Select(p => new PermissionItemViewModel
                    {
                        Value = p.Value,
                        Label = p.Label,
                        IsSelected = currentPerms.Contains(p.Value)
                    }).ToList()
                }).ToList();

            return View(new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Groups = grouped
            });
        }

        // ===== PERMISSIONS POST: lưu quyền =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(string roleId, List<string>? selectedPermissions)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            // Xóa tất cả quyền cũ từ AspNetRoleClaims
            var oldClaims = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == "Quyen")
                .ToList();
            foreach (var c in oldClaims)
                await _roleManager.RemoveClaimAsync(role, c);

            // Gán quyền mới vào AspNetRoleClaims
            if (selectedPermissions != null)
                foreach (var perm in selectedPermissions)
                    await _roleManager.AddClaimAsync(role, new Claim("Quyen", perm));

            TempData["Success"] = $"Đã cập nhật quyền cho role '{role.Name}'.";
            return RedirectToAction(nameof(Index));
        }
        // Thêm vào RolesController
        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) return Ok(new List<string>());

            var claims = await _roleManager.GetClaimsAsync(role);
            var perms = claims
                .Where(c => c.Type == "Quyen")
                .Select(c => c.Value)
                .ToList();

            return Ok(perms);
        }
    }

    // ===== VIEW MODELS =====
    public class RoleIndexViewModel
    {
        public string RoleId { get; set; } = "";
        public string RoleName { get; set; } = "";
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
    }

    public class RolePermissionsViewModel
    {
        public string RoleId { get; set; } = "";
        public string RoleName { get; set; } = "";
        public List<PermissionGroupViewModel> Groups { get; set; } = new();
    }

    public class PermissionGroupViewModel
    {
        public string GroupName { get; set; } = "";
        public List<PermissionItemViewModel> Items { get; set; } = new();
    }

    public class PermissionItemViewModel
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}
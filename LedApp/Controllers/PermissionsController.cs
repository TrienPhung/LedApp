using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LedApp.Data;
using LedApp.Models;

namespace LedApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PermissionsController : Controller
    {
        private readonly ApplicationDBContext _context;
        public PermissionsController(ApplicationDBContext context) => _context = context;

        // ===== INDEX =====
        public async Task<IActionResult> Index()
        {
            var perms = await _context.Permissions
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Order)
                .ToListAsync();

            var grouped = perms
                .GroupBy(p => p.GroupName)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(grouped);
        }

        // ===== CREATE GET =====
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách GroupName đã có để gợi ý
            var groups = await _context.Permissions
                .Select(p => p.GroupName)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.ExistingGroups = groups;
            return View(new Permission());
        }

        // ===== CREATE POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permission permission)
        {
            // Kiểm tra Value trùng
            if (await _context.Permissions.AnyAsync(p => p.Value == permission.Value.Trim()))
            {
                ModelState.AddModelError(nameof(permission.Value),
                    $"Mã quyền '{permission.Value}' đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ExistingGroups = await _context.Permissions
                    .Select(p => p.GroupName).Distinct().OrderBy(g => g).ToListAsync();
                return View(permission);
            }

            permission.Value = permission.Value.Trim();
            permission.GroupName = permission.GroupName.Trim();
            permission.Label = permission.Label.Trim();

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm quyền '{permission.Label}'.";
            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT GET =====
        public async Task<IActionResult> Edit(int id)
        {
            var perm = await _context.Permissions.FindAsync(id);
            if (perm == null) return NotFound();

            var groups = await _context.Permissions
                .Select(p => p.GroupName).Distinct().OrderBy(g => g).ToListAsync();
            ViewBag.ExistingGroups = groups;

            return View(perm);
        }

        // ===== EDIT POST =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Permission permission)
        {
            if (id != permission.Id) return NotFound();

            if (await _context.Permissions.AnyAsync(p =>
                p.Value == permission.Value.Trim() && p.Id != id))
            {
                ModelState.AddModelError(nameof(permission.Value),
                    $"Mã quyền '{permission.Value}' đã tồn tại.");
                ViewBag.ExistingGroups = await _context.Permissions
                    .Select(p => p.GroupName).Distinct().OrderBy(g => g).ToListAsync();
                return View(permission);
            }

            // Dùng AsTracking + FirstOrDefaultAsync thay vì FindAsync
            var existing = await _context.Permissions
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existing == null) return NotFound();

            existing.GroupName = permission.GroupName.Trim();
            existing.Value = permission.Value.Trim();
            existing.Label = permission.Label.Trim();
            existing.Order = permission.Order;
            existing.IsActive = permission.IsActive;
            existing.IsExtraOnly = permission.IsExtraOnly;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật quyền '{existing.Label}'.";
            return RedirectToAction(nameof(Index));
        }

        // ===== TOGGLE ACTIVE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var perm = await _context.Permissions
                .AsTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (perm == null) return NotFound();

            perm.IsActive = !perm.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = perm.IsActive
                ? $"Đã bật quyền '{perm.Label}'."
                : $"Đã tắt quyền '{perm.Label}'.";

            return RedirectToAction(nameof(Index));
        }

        // ===== DELETE =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var perm = await _context.Permissions.FindAsync(id);
            if (perm == null) return NotFound();

            _context.Permissions.Remove(perm);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa quyền '{perm.Label}'.";
            return RedirectToAction(nameof(Index));
        }

        // ===== BULK DELETE =====
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeletePermissionRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào." });

            var items = _context.Permissions.Where(p => request.Ids.Contains(p.Id));
            _context.Permissions.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa {request.Ids.Count} quyền." });
        }
        // GET: /Permissions/GetPermissionLabels?values=Xuat.View&values=Xuat.Create
        // PermissionsController
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPermissionLabels([FromQuery] List<string> values)
        {
            if (!values.Any()) return Ok(new List<object>());

            var perms = await _context.Permissions
                .Where(p => values.Contains(p.Value))
                .Select(p => new { value = p.Value, label = p.Label, groupName = p.GroupName }) // ← thêm groupName
                .ToListAsync();

            var result = values.Select(v => new {
                value = v,
                label = perms.FirstOrDefault(p => p.value == v)?.label ?? v,
                groupName = perms.FirstOrDefault(p => p.value == v)?.groupName ?? v.Split('.')[0] // ← thêm
            });

            return Ok(result);
        }
    }

    public class BulkDeletePermissionRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
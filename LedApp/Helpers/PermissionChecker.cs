using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Data;

namespace LedApp.Helpers
{
    public static class PermissionChecker
    {
        /// <summary>
        /// Kiểm tra user có quyền không.
        /// Thứ tự ưu tiên:
        /// 1. Admin → bypass tất cả
        /// 2. QuyenDeny (cá nhân) → chặn dù role có
        /// 3. QuyenExtra (cá nhân) → cho qua
        /// 4. RoleClaims (AspNetRoleClaims) → kế thừa từ role
        /// 5. Không có → chặn
        /// </summary>
        public static async Task<bool> HasPermission(
            ClaimsPrincipal userPrincipal,
            string permission,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1. Admin bypass tất cả
            if (userPrincipal.IsInRole("Admin")) return true;

            // Lấy AppUser từ ClaimsPrincipal
            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return false;

            // Lấy tất cả UserClaims
            var userClaims = await userManager.GetClaimsAsync(user);

            // 2. Bị deny cá nhân → chặn dù role có quyền
            if (userClaims.Any(c => c.Type == "QuyenDeny" && c.Value == permission))
                return false;

            // 3. Được grant thêm cá nhân → cho qua
            if (userClaims.Any(c => c.Type == "QuyenExtra" && c.Value == permission))
                return true;

            // 4. Kế thừa từ Role → đọc AspNetRoleClaims
            var roles = await userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var roleClaims = await roleManager.GetClaimsAsync(role);
                if (roleClaims.Any(c => c.Type == "Quyen" && c.Value == permission))
                    return true;
            }

            // 5. Không có → chặn
            return false;
        }

        /// <summary>
        /// Kiểm tra user có được dùng cửa xuất không
        /// </summary>
        public static async Task<bool> HasCuaXuat(
            ClaimsPrincipal userPrincipal,
            int cuaXuatId,
            UserManager<AppUser> userManager)
        {
            // Admin bypass
            if (userPrincipal.IsInRole("Admin")) return true;

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return false;

            var userClaims = await userManager.GetClaimsAsync(user);
            return userClaims.Any(c =>
                c.Type == "CuaXuat" &&
                c.Value == cuaXuatId.ToString());
        }

        /// <summary>
        /// Kiểm tra user có được dùng cửa nhập không
        /// </summary>
        public static async Task<bool> HasCuaNhap(
            ClaimsPrincipal userPrincipal,
            int cuaNhapId,
            UserManager<AppUser> userManager)
        {
            // Admin bypass
            if (userPrincipal.IsInRole("Admin")) return true;

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return false;

            var userClaims = await userManager.GetClaimsAsync(user);
            return userClaims.Any(c =>
                c.Type == "CuaNhap" &&
                c.Value == cuaNhapId.ToString());
        }

        /// <summary>
        /// Lấy danh sách ID cửa xuất user được dùng
        /// </summary>
        public static async Task<List<int>> GetAllowedCuaXuatIds(
            ClaimsPrincipal userPrincipal,
            UserManager<AppUser> userManager,
            ApplicationDBContext context)
        {
            // Admin được dùng tất cả
            if (userPrincipal.IsInRole("Admin"))
                return await context.CuaXuats.Select(c => c.Id).ToListAsync();

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return new List<int>();

            var userClaims = await userManager.GetClaimsAsync(user);
            return userClaims
                .Where(c => c.Type == "CuaXuat")
                .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        /// <summary>
        /// Lấy danh sách ID cửa nhập user được dùng
        /// </summary>
        public static async Task<List<int>> GetAllowedCuaNhapIds(
            ClaimsPrincipal userPrincipal,
            UserManager<AppUser> userManager,
            ApplicationDBContext context)
        {
            // Admin được dùng tất cả
            if (userPrincipal.IsInRole("Admin"))
                return await context.CuaNhaps.Select(c => c.Id).ToListAsync();

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return new List<int>();

            var userClaims = await userManager.GetClaimsAsync(user);
            return userClaims
                .Where(c => c.Type == "CuaNhap")
                .Select(c => int.TryParse(c.Value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        /// <summary>
        /// Lấy tất cả quyền hiện tại của user
        /// (bao gồm từ role + extra cá nhân, trừ deny)
        /// </summary>
        public static async Task<List<string>> GetAllPermissions(
            ClaimsPrincipal userPrincipal,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            var result = new HashSet<string>();

            var user = await userManager.GetUserAsync(userPrincipal);
            if (user == null) return result.ToList();

            var userClaims = await userManager.GetClaimsAsync(user);
            var denyList = userClaims
                .Where(c => c.Type == "QuyenDeny")
                .Select(c => c.Value)
                .ToHashSet();

            // Quyền từ role
            var roles = await userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null) continue;
                var roleClaims = await roleManager.GetClaimsAsync(role);
                foreach (var c in roleClaims.Where(c => c.Type == "Quyen"))
                    if (!denyList.Contains(c.Value))
                        result.Add(c.Value);
            }

            // Quyền extra cá nhân
            foreach (var c in userClaims.Where(c => c.Type == "QuyenExtra"))
                result.Add(c.Value);

            return result.ToList();
        }
    }
}
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LedApp.Helpers
{
    // Requirement chung cho mọi Policy
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
            => Permission = permission;
    }

    // Handler xử lý logic kiểm tra quyền
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionHandler(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Admin bypass tất cả
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null) return;

            var userClaims = await _userManager.GetClaimsAsync(user);

            // Bị Deny cá nhân → chặn
            if (userClaims.Any(c =>
                c.Type == "QuyenDeny" &&
                c.Value == requirement.Permission))
                return;

            // Được Extra cá nhân → cho qua
            if (userClaims.Any(c =>
                c.Type == "QuyenExtra" &&
                c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }

            // Kế thừa từ Role
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                if (roleClaims.Any(c =>
                    c.Type == "Quyen" &&
                    c.Value == requirement.Permission))
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}
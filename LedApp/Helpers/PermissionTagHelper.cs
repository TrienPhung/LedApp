using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using LedApp.Models;

namespace LedApp.Helpers
{
    /// <summary>
    /// Tag Helper cho phép dùng thuộc tính perm="XuatKho.Delete" trên bất kỳ element nào.
    ///
    /// Cách dùng trong Razor View:
    ///   <button perm="Xuat.Delete">Xóa</button>
    ///   <a perm="Xuat.Create" asp-action="Create">Thêm mới</a>
    ///   <button perm="Xuat.Edit">Sửa</button>
    ///
    /// Nếu CÓ quyền  → element hiển thị bình thường
    /// Nếu KHÔNG quyền:
    ///   - Thêm class "perm-disabled"
    ///   - Thêm attribute disabled (nếu là button/input)
    ///   - Wrap bằng div.perm-tooltip-wrap để hiện tooltip khi hover
    ///   - Nếu là thẻ <a> → href bị xóa, thêm aria-disabled
    /// </summary>
    [HtmlTargetElement(Attributes = "perm")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Thuộc tính perm="XuatKho.Delete"
        public string Perm { get; set; } = "";

        // Tooltip tùy chỉnh (mặc định = "Bạn không có quyền thực hiện thao tác này")
        public string PermTooltip { get; set; } = "Bạn không có quyền thực hiện thao tác này";

        public PermissionTagHelper(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Xóa attribute perm và perm-tooltip khỏi HTML output (không cần hiện ra)
            output.Attributes.RemoveAll("perm");
            output.Attributes.RemoveAll("perm-tooltip");

            if (string.IsNullOrWhiteSpace(Perm)) return;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true) return;

            // Gọi static method, không đụng gì PermissionChecker.cs
            var hasPermission = await PermissionChecker.HasPermission(
                user, Perm, _userManager, _roleManager);

            if (hasPermission) return; // Có quyền → không làm gì

            // ── KHÔNG CÓ QUYỀN: áp dụng disabled + tooltip ──

            var tagName = output.TagName?.ToLower() ?? "";

            // 1. Thêm class perm-disabled
            var existingClass = output.Attributes["class"]?.Value?.ToString() ?? "";
            output.Attributes.SetAttribute("class", (existingClass + " perm-disabled").Trim());

            // 2. Nếu là button/input → thêm disabled
            if (tagName is "button" or "input")
            {
                output.Attributes.SetAttribute("disabled", "disabled");
            }

            // 3. Nếu là thẻ <a> → xóa href, thêm aria-disabled
            if (tagName == "a")
            {
                output.Attributes.RemoveAll("href");
                output.Attributes.SetAttribute("href", "javascript:void(0)");
                output.Attributes.SetAttribute("aria-disabled", "true");
            }

            // 4. Ngăn click bằng onclick
            output.Attributes.SetAttribute("onclick", "return false;");

            // 5. Wrap element trong div.perm-tooltip-wrap để hiện tooltip
            output.PreElement.SetHtmlContent(
                $"<div class=\"perm-tooltip-wrap\" data-tooltip=\"{System.Net.WebUtility.HtmlEncode(PermTooltip)}\">");
            output.PostElement.SetHtmlContent("</div>");
        }
    }
}
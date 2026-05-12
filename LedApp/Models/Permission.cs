using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("Permissions")]
    public class Permission
    {
        public int Id { get; set; }

        // Nhóm hiển thị trên UI
        // VD: "Xuất kho", "Nhập kho", "Báo cáo"
        public string GroupName { get; set; } = "";

        // Mã quyền — dùng để check trong code
        // VD: "Xuat.View", "Xuat.Create"
        public string Value { get; set; } = "";

        // Tên hiển thị thân thiện trên UI
        // VD: "Xem phiếu xuất", "Tạo phiếu xuất"
        public string Label { get; set; } = "";

        // Thứ tự hiển thị trong group
        public int Order { get; set; }

        // Ẩn/hiện trên UI — không xóa hẳn khỏi DB
        public bool IsActive { get; set; } = true;

        // true = chỉ dùng cho Extra cá nhân, không hiện trong Role
        // false = quyền chung, gán vào Role bình thường
        public bool IsExtraOnly { get; set; } = false;
    }
}

using System.ComponentModel;

namespace LedApp.Models
{
    public class NguoiDung
    {
        public Guid Id { get; set; }
        [DisplayName("Tên đăng nhập")]
        public string Username { get; set; }
        [DisplayName("Mật khẩu")]
        public string Password { get; set; }
        [DisplayName("Họ tên người dùng")]
        public string Name { get; set; }
        [DisplayName("Ảnh đại diện")]
        public string Image { get; set; }
        [DisplayName("Điện thoại")]
        public string Tels { get; set; }
        [DisplayName("Emai")]
        public string Email { get; set; }
        [DisplayName("Quyền truy cập")]
        public int Quyen { get; set; }
    }
}

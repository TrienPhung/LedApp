using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("nguoiDungs")]
    public class nguoiDungs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DisplayName("Tên đăng nhập")]
        public string Username { get; set; }

        [DisplayName("Mật khẩu")]
        public string Password { get; set; }

        [DisplayName("Họ tên")]
        public string Name { get; set; }

        [DisplayName("Điện thoại")]
        public string Tels { get; set; }

        [DisplayName("Email")]
        public string Email { get; set; }

        [DisplayName("Quyền truy cập")]
        public int Quyen { get; set; }

        // Navigation properties
        public ICollection<Nhap>? Nhaps { get; set; }
        public ICollection<Xuat>? Xuats { get; set; }
        public ICollection<LichSuBanGiao>? LichSuBanGiaos { get; set; }
    }
}
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
        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [DisplayName("Mật khẩu")]
        [Required]
        [MaxLength(255)]
        public string Password { get; set; }

        [DisplayName("Họ tên")]
        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [DisplayName("Điện thoại")]
        [MaxLength(20)]
        public string? Tels { get; set; }

        [DisplayName("Email")]
        [MaxLength(150)]
        [EmailAddress]
        public string? Email { get; set; }

        [DisplayName("Quyền truy cập")]
        public int Quyen { get; set; }

        [DisplayName("Ảnh đại diện")]
        [MaxLength(500)]
        public string? Image { get; set; }

        // Navigation properties
        public ICollection<Nhap>? Nhaps { get; set; }
        public ICollection<Xuat>? Xuats { get; set; }
        public ICollection<LichSuBanGiao>? LichSuBanGiaos { get; set; }
    }
}
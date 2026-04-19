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


        [MaxLength(450)]
        public string? UserId { get; set; }

        [DisplayName("Họ")]
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [DisplayName("Tên")]
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [DisplayName("Ảnh đại diện")]
        [MaxLength(500)]
        public string? Image { get; set; }

        [DisplayName("Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [DisplayName("Số điện thoại")]
        [MaxLength(15)]
        [Phone]
        public string? SoDienThoai { get; set; }

        [DisplayName("Địa chỉ")]
        [MaxLength(255)]
        public string? DiaChi { get; set; }

        [DisplayName("Giới tính")]
        public GioiTinhEnum? GioiTinh { get; set; }

        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}";
        //hoặc viết cách này public string FullName => LastName + " " + FirstName;

        public ICollection<Nhap>? Nhaps { get; set; }
        public ICollection<Xuat>? Xuats { get; set; }
        public ICollection<LichSuBanGiao>? LichSuBanGiaos { get; set; }
    }
}
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LedApp.Models
{
    [Table("DiaDiem")]
    public class DiaDiem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Ten { get; set; } = "";

        [MaxLength(200)]
        public string? DiaChi { get; set; }

        [MaxLength(100)]
        public string? QuanHuyen { get; set; }

        [MaxLength(100)]
        public string? ThanhPho { get; set; }

        public double? Lat { get; set; }    // ← đổi thành nullable
        public double? Lng { get; set; }    // ← đổi thành nullable
                                            // vì lúc mới tạo chưa có tọa độ
                                            // sẽ lấy sau qua Geocoding

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now; // ← thêm

        // Navigation
        public ICollection<Xuat>? Xuats { get; set; }         // ← thêm
        public ICollection<HanhTrinh>? HanhTrinhs { get; set; }
    }
}

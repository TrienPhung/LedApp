using System.ComponentModel.DataAnnotations;

namespace Viettel_post_API.Models
{
    public class DanhSachXe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BienSo { get; set; } = "";

        public string? TenLaiXe { get; set; }

        public string? MaChiNhanh { get; set; }

        public bool IsHienThi { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        // 🔥 Quan hệ 1 - N
        public List<ChuyenXe> ChuyenXes { get; set; } = new();
    }
}
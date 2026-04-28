using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Viettel_post_API.Models
{
    public class ChuyenXe
    {
        [Key]
        public int Id { get; set; }

        // 🔥 Khóa ngoại tới xe
        public int DanhSachXeId { get; set; }

        [ForeignKey("DanhSachXeId")]
        [JsonIgnore]
        public DanhSachXe DanhSachXe { get; set; } = null!;

        // 🔥 ID từ API Viettel (quan trọng)
        [Required]
        public string MaChuyenApi { get; set; } = "";

        public int LanThu { get; set; }  // chuyến thứ mấy trong ngày

        // Thời gian
        public DateTime NgayDuKien { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }

        // Trạng thái
        public TrangThaiChuyen TrangThai { get; set; } = TrangThaiChuyen.ChuaVe;

        // Sync
        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public DateTime ThoiGianSync { get; set; } = DateTime.UtcNow;

        // 🔥 Dùng để hiển thị nhanh chuyến mới nhất
        public bool IsLatest { get; set; } = true;

        // 🔥 Quan hệ 1 - N
        public List<HangHoaItem> HangHoas { get; set; } = new();
    }
}
// Models/DanhSachXe.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("DanhSachXe")]
    public class DanhSachXe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [DisplayName("Biển số xe")]
        public string BienSoXe { get; set; } = string.Empty;

        [DisplayName("Loại xe")]
        public string LoaiXe { get; set; } = string.Empty;

        [DisplayName("Tải trọng")]
        public long TaiTrong { get; set; }

        [DisplayName("Trạng thái")]
        public string TrangThai { get; set; } = string.Empty;

        [DisplayName("Ghi chú")]
        public string? GhiChu { get; set; }

        [DisplayName("Thời gian dự kiến về")]
        public DateTime? ThoiGianDuKienVe { get; set; }

        // Navigation
        public ICollection<Xuat>? Xuats { get; set; }
    }
}
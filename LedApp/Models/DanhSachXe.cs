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
        public int TrangThai { get; set; } = (int)TrangThaiXe.TrongBai;

        [DisplayName("Ghi chú")]
        public string? GhiChu { get; set; }

        // ← THÊM MỚI: FK trỏ vào bảng nguoiDungs
        [ForeignKey(nameof(TaiXe))]
        [DisplayName("Tài xế")]
        public int? TaiXeId { get; set; }


        public nguoiDungs? TaiXe { get; set; }  // ← navigation property, tên TaiXe là do mình đặt
        // Navigation
        public ICollection<Xuat>? Xuats { get; set; }
    }
}
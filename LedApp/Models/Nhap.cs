using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("Nhap")]
    public class Nhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(CuaNhap))]
        [DisplayName("Cửa nhập")]
        public int CuaNhapId { get; set; }

        [DisplayName("Biển số xe")]
        public string BienSoXe { get; set; }

        [DisplayName("Thời gian phân công")]
        public DateTime ThoiGianPhanCong { get; set; }

        [DisplayName("Thời gian vào bãi")]
        public DateTime? ThoiGianVaoBai { get; set; }

        [DisplayName("Thời gian vào cửa")]
        public DateTime? ThoiGianVaoCua { get; set; }

        [DisplayName("Thời gian giới hạn")]
        public DateTime? ThoiGianGioiHan { get; set; }

        [DisplayName("Thời gian hoàn thành")]
        public DateTime? ThoiGianHoanThanh { get; set; }

        [ForeignKey(nameof(NhanVienXacNhan))]
        [DisplayName("Nhân viên xác nhận")]
        public int? NhanVienXacNhanId { get; set; }

        [DisplayName("Trạng thái")]
        // Nhap.cs
        public int TrangThai { get; set; } = (int)TrangThaiNhap.DaPhanCong;
        [DisplayName("Ghi chú")]
        [MaxLength(500)]
        public string? GhiChu { get; set; }
        // Navigation properties
        public CuaNhap? CuaNhap { get; set; }
        public nguoiDungs? NhanVienXacNhan { get; set; }
        public ICollection<ChitietNhap>? ChitietNhaps { get; set; }
    }
}
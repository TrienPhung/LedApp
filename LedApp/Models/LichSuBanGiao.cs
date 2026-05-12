// Models/LichSuBanGiao.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("LichSuBanGiao")]
    public class LichSuBanGiao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // "NHAP" hoặc "XUAT" — phân biệt loại phiếu
        [DisplayName("Loại phiếu")]
        public string LoaiPhieu { get; set; } = string.Empty;

        // ID của Nhap.Id hoặc Xuat.Id tuỳ LoaiPhieu
        [DisplayName("Mã phiếu")]
        public int PhieuId { get; set; }

        // ID của ChiTietDonViNhap.Id hoặc ChiTietDonViXuat.Id
        [DisplayName("Mã chi tiết đơn vị")]
        [Column("ChitietDonViId")]
        public int? ChiTietDonViId { get; set; }

        [DisplayName("Đơn vị")]
        public string DonVi { get; set; } = string.Empty;

        [DisplayName("Số bàn giao")]
        public long SoBG { get; set; }

        [ForeignKey(nameof(NhanVien))]
        [DisplayName("Nhân viên")]
        public int? NhanVienId { get; set; }

        [DisplayName("Thời gian")]
        public DateTime ThoiGian { get; set; }

        // Navigation
        public nguoiDungs? NhanVien { get; set; }
    }
}
// Models/Xuat.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("Xuat")]
    public class Xuat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(CuaXuat))]
        [DisplayName("Cửa xuất")]
        public int CuaXuatId { get; set; }

        //Link tới DanhSachXe — khác với Nhap chỉ lưu BienSoXe string
        [ForeignKey(nameof(Xe))]
        [DisplayName("Xe")]
        public int XeId { get; set; }

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

        [DisplayName("Thời gian xuất phát")]
        public DateTime? ThoiGianXuatPhat { get; set; }

        [ForeignKey(nameof(NhanVienXacNhan))]
        [DisplayName("Nhân viên xác nhận")]
        public int? NhanVienXacNhanId { get; set; }

        [DisplayName("Trạng thái")]
        public string TrangThai { get; set; } = "DaPhanCong";

        // Navigation properties
        public CuaXuat? CuaXuat { get; set; }
        public DanhSachXe? Xe { get; set; }
        public nguoiDungs? NhanVienXacNhan { get; set; }
        public ICollection<ChitietXuat>? ChitietXuats { get; set; }
    }
}
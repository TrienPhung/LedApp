// Models/CanhBao.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("CanhBao")]
    public class CanhBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // ✅ "NHAP" hoặc "XUAT"
        [DisplayName("Loại phiếu")]
        public string LoaiPhieu { get; set; } = string.Empty;

        [DisplayName("Mã phiếu")]
        public int PhieuId { get; set; }

        // Ví dụ: "QuaThoiGian"
        [DisplayName("Loại cảnh báo")]
        public string LoaiCanhBao { get; set; } = string.Empty;

        [DisplayName("Thời gian")]
        public DateTime ThoiGian { get; set; }

        [DisplayName("Ghi chú")]
        public string? GhiChu { get; set; }
    }
}
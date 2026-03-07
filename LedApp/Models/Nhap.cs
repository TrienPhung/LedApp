
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
        [DisplayName("Cửa nhập số")]
        public int CuaNhapId { get; set; }
        [DisplayName("Biển số xe")]
        public string BienSoXe { get; set; }
        [DisplayName("Thời gian nhập")]
        public DateTime NgayNhap { get; set; }
        [DisplayName("Giờ nhập")]
        public int? GioNhap { get; set; }
        [DisplayName("Phút nhập")]
        public int? PhutNhap { get; set; }
        [DisplayName("Cổng vào")]
        public int CongVao { get; set; }
        [DefaultValue(false)]
        [DisplayName("Trạng thái")]
        public bool TrangThai { get; set; }
        public CuaNhap? CuaNhap { get; set; }
        public ICollection<ChitietNhap> ChitietNhaps { get; set; }

    }
}

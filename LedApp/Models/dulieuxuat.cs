using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("dulieuxuat")]
    public class dulieuxuat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [ForeignKey(nameof(CuaXuat))]
        [DisplayName("Cửa xuất số")]
        public int CuaXuatId { get; set; }
        [DisplayName("Biển số xe")]
        public string BienSoXe { get; set; }
        [DisplayName("Ngày giờ xuất")]
        public DateTime NgayXuat { get; set; }
        [DisplayName("Giờ xuất")]
        public int? GioXuat { get; set; }
        [DisplayName("Phút xuất")]
        public int? PhutXuat { get; set; }
        [DisplayName("Cổng ra")]
        public int CongRa { get; set; }
        [DefaultValue(false)]
        [DisplayName("Trạng thái")]
        public bool TrangThai { get; set; }
        public CuaXuat? CuaXuat { get; set; }
        public ICollection<ChitietXuat> ChitietXuat { get; set; }
    }
}

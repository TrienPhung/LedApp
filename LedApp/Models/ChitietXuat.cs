using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("ChitietXuat")]
    public class ChitietXuat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        //public string TenCua{ get; set; }
        //public string BienSo { get; set; }
        [DisplayName("Số lượng chưa xuất")]
        public long SoluongCH { get; set; }
        [DisplayName("Đơn vị")]
        public string donviCH { get; set; }
        [DisplayName("Số lượng đã xuất")]
        public long SoluongD { get; set; }
        [DisplayName("Đơn vị")]
        public string donviD { get; set; }
        [ForeignKey(nameof(dulieuxuat))]
        [DisplayName("Mã dữ liệu xuất")]
        public int dulieuxuatId { get; set; }
        //public dulieuxuat? dulieuxuat { get; set; }
    }
}

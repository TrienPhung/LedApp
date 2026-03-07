using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("ChitietNhap")]
    public class ChitietNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [ForeignKey(nameof(Nhap))]
        [DisplayName("Mã dữ liệu nhập")]
        public int NhapId { get; set; }
        //public string BienSo { get; set; }
        [DisplayName("Số lượng nhập")]
        public long Soluong { get; set; }
        [DisplayName("Đơn vị")]
        public string donvi { get; set; }
        //public Nhap Nhap { get; set; }
    }
}

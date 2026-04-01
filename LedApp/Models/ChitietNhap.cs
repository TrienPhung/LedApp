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
        [DisplayName("Mã phiếu nhập")]
        public int NhapId { get; set; }

        [DisplayName("Đơn vị")]
        public string DonVi { get; set; }

        [DisplayName("Chưa bàn giao")]
        public long ChuaBG { get; set; }

        [DisplayName("Đã bàn giao")]
        public long DaBG { get; set; }

        // Navigation property
        public Nhap? Nhap { get; set; }
    }
}
// Models/ChiTietDonViXuat.cs
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

        [ForeignKey(nameof(Xuat))]
        [DisplayName("Mã phiếu xuất")]
        public int XuatId { get; set; }

        [DisplayName("Đơn vị")]
        public string DonVi { get; set; } = string.Empty;

        [DisplayName("Chưa bàn giao")]
        public long ChuaBG { get; set; }

        [DisplayName("Đã bàn giao")]
        public long DaBG { get; set; }

        // Navigation
        public Xuat? Xuat { get; set; }
    }
}
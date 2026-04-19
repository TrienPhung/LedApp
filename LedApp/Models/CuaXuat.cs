using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("CuaXuat")]
    public class CuaXuat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DisplayName("Tên cửa")]
        public string Ten { get; set; }
        [DisplayName("Mô tả")]
        public string Mota { get; set; }
        [DisplayName("Trạng thái")]
        public bool IsActive { get; set; } = true;
        public ICollection<Xuat> Xuats { get; set; }
    }
}

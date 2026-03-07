using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("CuaNhap")]
    public class CuaNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Ten { get; set; }
        public string Mota { get; set; }
        public ICollection<Nhap> Nhaps { get; set; }
    }
}

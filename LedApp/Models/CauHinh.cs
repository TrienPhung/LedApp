// Models/CauHinh.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LedApp.Models
{
    [Table("CauHinhs")]
    public class CauHinh
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;   // VD: "ThoiGianGioiHanNhap"
        public string Value { get; set; } = string.Empty; // VD: "40"
    }
}
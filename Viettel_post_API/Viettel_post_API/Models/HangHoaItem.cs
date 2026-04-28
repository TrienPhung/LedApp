using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Viettel_post_API.Models
{
    public class HangHoaItem
    {
        [Key]
        public int Id { get; set; }

        // 🔥 Gắn với CHUYẾN (QUAN TRỌNG NHẤT)
        public int ChuyenXeId { get; set; }

        [ForeignKey("ChuyenXeId")]
        [JsonIgnore]
        public ChuyenXe ChuyenXe { get; set; } = null!;

        [Required]
        public string DonVi { get; set; } = ""; // Ví dụ: kiện, bao, pallet

        public long SoLuong { get; set; }
    }
}
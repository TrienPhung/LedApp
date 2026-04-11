using System.ComponentModel.DataAnnotations;

namespace LedApp.Models
{
    public enum TrangThaiXe
    {
        [Display(Name = "Trong bãi")]
        TrongBai = 0,
        [Display(Name = "Đang phân công")]
        DangPhanCong = 1,

        [Display(Name = "Đang vận chuyển")]
        DangVanChuyen = 2,

        [Display(Name = "Bảo trì")]
        BaoTri = 3 
    }
}
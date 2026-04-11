namespace LedApp.Models
{
    public static class TrangThaiHelper
    {
        public static string GetTenTrangThaiNhap(int trangThai)
        {
            return trangThai switch
            {
                (int)TrangThaiNhap.DaPhanCong => "Đã phân công",
                (int)TrangThaiNhap.DangBanGiao => "Đang bàn giao",
                (int)TrangThaiNhap.QuaThoiGian => "Quá thời gian",
                (int)TrangThaiNhap.HoanThanh => "Hoàn thành",
                _ => "Không xác định"
            };
        }

        public static string GetBadgeClassNhap(int trangThai)
        {
            return trangThai switch
            {
                (int)TrangThaiNhap.DaPhanCong => "badge-secondary",
                (int)TrangThaiNhap.DangBanGiao => "badge-primary",
                (int)TrangThaiNhap.QuaThoiGian => "badge-danger",
                (int)TrangThaiNhap.HoanThanh => "badge-success",
                _ => "badge-secondary"
            };
        }
    }
}
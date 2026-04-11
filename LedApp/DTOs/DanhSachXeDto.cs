namespace LedApp.DTOs
{
    public class DanhSachXeDto
    {
        public int Id { get; set; }
        public string BienSo { get; set; } = "";
        public string? TenLaiXe { get; set; }
        public string? MaChiNhanh { get; set; }
        public bool IsHienThi { get; set; }
        public ChuyenXeDto? ChuyenHienTai { get; set; }
    }
}
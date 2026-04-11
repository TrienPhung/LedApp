namespace LedApp.DTOs
{
    public class ChuyenXeDto
    {
        public int Id { get; set; }
        public int DanhSachXeId { get; set; }
        public string MaChuyenApi { get; set; } = "";
        public int LanThu { get; set; }
        public DateTime NgayDuKien { get; set; }
        public DateTime? ThoiGianDen { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }
        public int TrangThai { get; set; }  // nhận int từ API
        public bool IsLatest { get; set; }
        public List<HangHoaItemDto> HangHoas { get; set; } = new();
    }
}
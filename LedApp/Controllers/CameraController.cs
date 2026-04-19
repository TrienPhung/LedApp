//using Microsoft.AspNetCore.Mvc;
//using LedApp.DTOs;
//using LedApp.Hubs;
//using Microsoft.AspNetCore.SignalR;
//using Newtonsoft.Json;

//namespace LedApp.Controllers
//{
//    [Route("[controller]")]
//    public class CameraController : Controller
//    {
//        private readonly IHubContext<SignalServer> _hubContext;
//        private readonly ILogger<CameraController> _logger;
//        private readonly IHttpClientFactory _httpClientFactory;

//        public CameraController(
//            IHubContext<SignalServer> hubContext,
//            ILogger<CameraController> logger,
//            IHttpClientFactory httpClientFactory)
//        {
//            _hubContext = hubContext;
//            _logger = logger;
//            _httpClientFactory = httpClientFactory;
//        }

//        [HttpGet("")]
//        public IActionResult Index() => View();

//        [HttpPost("NhanDienBienSo")]
//        public async Task<IActionResult> NhanDienBienSo([FromBody] NhanDienRequest req)
//        {
//            if (string.IsNullOrWhiteSpace(req.BienSo))
//                return BadRequest(new { message = "Biển số trống!" });

//            // Chuẩn hóa biển số
//            var bienSo = req.BienSo
//                .ToUpper()
//                .Replace(".", "")
//                .Replace("-", "")
//                .Trim();

//            _logger.LogInformation("[Camera] Nhận biển số: {BienSo} | Confidence: {Conf}", bienSo, req.Confidence);

//            var now = DateTime.Now;

//            // Lấy danh sách xe từ API
//            var xeTimThay = await TimXeTuApi(bienSo);

//            if (xeTimThay != null)
//            {
//                var cx = xeTimThay.ChuyenHienTai;
//                var ketQua = new KetQuaNhanDien
//                {
//                    BienSo = req.BienSo,
//                    TimThay = true,
//                    LoaiPhieu = "XE",
//                    PhieuId = cx?.Id,
//                    TenCua = cx?.MaChuyenApi ?? "--",
//                    TrangThai = cx?.TrangThai ?? 0,
//                    ThoiGian = now,
//                    Confidence = req.Confidence,
//                    ThongBao = $"Xe {xeTimThay.BienSo} — {xeTimThay.TenLaiXe ?? "--"} — {TrangThaiLabel(cx?.TrangThai ?? -1)}"
//                };

//                await _hubContext.Clients.All.SendAsync("CameraNhanDien", ketQua);
//                return Ok(new { message = ketQua.ThongBao, data = ketQua });
//            }

//            // Không tìm thấy
//            var khongTimThay = new KetQuaNhanDien
//            {
//                BienSo = req.BienSo,
//                TimThay = false,
//                LoaiPhieu = "--",
//                PhieuId = null,
//                TenCua = "--",
//                TrangThai = -1,
//                ThoiGian = now,
//                Confidence = req.Confidence,
//                ThongBao = $"Xe {req.BienSo} không có trong danh sách xe đang về!"
//            };

//            await _hubContext.Clients.All.SendAsync("CameraNhanDien", khongTimThay);
//            _logger.LogWarning("[Camera] Không tìm thấy biển số: {BienSo}", bienSo);
//            return Ok(new { message = khongTimThay.ThongBao, data = khongTimThay });
//        }

//        private async Task<DanhSachXeDto?> TimXeTuApi(string bienSoChuanHoa)
//        {
//            try
//            {
//                var client = _httpClientFactory.CreateClient("ViettelApi");
//                var response = await client.GetAsync("api/DanhSachXes/dashboard");
//                if (!response.IsSuccessStatusCode) return null;

//                var json = await response.Content.ReadAsStringAsync();
//                var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json) ?? new();

//                return xes.FirstOrDefault(x =>
//                    x.BienSo.Replace("-", "").Replace(".", "").ToUpper() == bienSoChuanHoa);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning("[Camera] Lỗi gọi API: {msg}", ex.Message);
//                return null;
//            }
//        }

//        [HttpGet("Status")]
//        public IActionResult Status() => Ok(new
//        {
//            status = "online",
//            time = DateTime.Now,
//            message = "ASP.NET Camera API sẵn sàng"
//        });

//        private static string TrangThaiLabel(int tt) => tt switch
//        {
//            0 => "Chưa về",
//            1 => "Đang về",
//            2 => "Đã đến",
//            3 => "Đang nhập hàng",
//            4 => "Hoàn thành",
//            _ => "--"
//        };
//    }

//    public class NhanDienRequest
//    {
//        public string BienSo { get; set; } = string.Empty;
//        public double Confidence { get; set; }
//        public string ThoiGian { get; set; } = string.Empty;
//        public string Nguon { get; set; } = "webcam";
//    }

//    public class KetQuaNhanDien
//    {
//        public string BienSo { get; set; } = string.Empty;
//        public bool TimThay { get; set; }
//        public string LoaiPhieu { get; set; } = string.Empty;
//        public int? PhieuId { get; set; }
//        public string TenCua { get; set; } = string.Empty;
//        public int TrangThai { get; set; }
//        public DateTime ThoiGian { get; set; }
//        public double Confidence { get; set; }
//        public string ThongBao { get; set; } = string.Empty;
//    }
//}
using LedApp.DTOs;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    [Authorize(Roles = "BaoVe")]
    public class BaoVeNhapController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<BaoVeNhapController> _logger;

        public BaoVeNhapController(
            IHttpClientFactory httpClientFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<BaoVeNhapController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
        }
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "";
            ViewBag.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            return View();
        }

        // ✅ Python gọi vào đây để gửi kết quả nhận diện
        // Bỏ [Authorize] riêng cho action này vì Python không login được
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> NhanDienBienSo([FromBody] NhanDienRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BienSo))
                return BadRequest(new { message = "Biển số trống!" });

            var bienSo = req.BienSo.ToUpper().Replace(".", "").Replace("-", "").Trim();
            _logger.LogInformation("[Camera] Nhận biển số: {BienSo} | Confidence: {Conf}", bienSo, req.Confidence);

            var now = DateTime.Now;
            var xeTimThay = await TimXeTuApi(bienSo);

            if (xeTimThay != null)
            {
                var cx = xeTimThay.ChuyenHienTai;
                var ketQua = new KetQuaNhanDien
                {
                    BienSo = req.BienSo,
                    TimThay = true,
                    LoaiPhieu = "XE",
                    PhieuId = cx?.Id,
                    TenCua = cx?.MaChuyenApi ?? "--",
                    TrangThai = cx?.TrangThai ?? 0,
                    ThoiGian = now,
                    Confidence = req.Confidence,
                    ThongBao = $"Xe {xeTimThay.BienSo} — {xeTimThay.TenLaiXe ?? "--"} — {TrangThaiLabel(cx?.TrangThai ?? -1)}"
                };
                await _hubContext.Clients.All.SendAsync("CameraNhanDien", ketQua);
                return Ok(new { message = ketQua.ThongBao, data = ketQua });
            }

            var khongTimThay = new KetQuaNhanDien
            {
                BienSo = req.BienSo,
                TimThay = false,
                LoaiPhieu = "--",
                PhieuId = null,
                TenCua = "--",
                TrangThai = -1,
                ThoiGian = now,
                Confidence = req.Confidence,
                ThongBao = $"Xe {req.BienSo} không có trong danh sách xe đang về!"
            };
            await _hubContext.Clients.All.SendAsync("CameraNhanDien", khongTimThay);
            return Ok(new { message = khongTimThay.ThongBao, data = khongTimThay });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Status() => Ok(new
        {
            status = "online",
            time = DateTime.Now,
            message = "Trạm bảo vệ sẵn sàng"
        });

        [HttpPost]
        public async Task<IActionResult> XacNhanVaoBai([FromBody] XacNhanVaoBaiRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.PutAsJsonAsync(
                    $"api/DanhSachXes/chuyen/{request.ChuyenId}/trangthai", 2);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { message = "Cập nhật trạng thái thất bại" });

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);

                int chuyenId = (int)result.chuyenId;
                int trangThai = (int)result.trangThai;
                string? thoiGianDen = result.thoiGianDen?.ToString();
                string? thoiGianHoanThanh = result.thoiGianHoanThanh?.ToString();

                await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated",
                    chuyenId, trangThai, thoiGianDen, thoiGianHoanThanh);

                _logger.LogInformation("Xe vào bãi OK: chuyenId={ChuyenId}, bienSo={BienSo}",
                    request.ChuyenId, request.BienSo);

                return Ok(new { success = true, message = $"Xe {request.BienSo} đã vào bãi", thoiGianDen });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XacNhanVaoBai chuyenId={ChuyenId}", request.ChuyenId);
                return StatusCode(500, new { message = "Lỗi server" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDanhSachXe()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                var json = await response.Content.ReadAsStringAsync();
                var allXes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                             ?? new List<DanhSachXeDto>();

                var dangVe = allXes
                    .Where(x => x.ChuyenHienTai?.TrangThai == 1)
                    .Select(x => new
                    {
                        chuyenId = x.ChuyenHienTai!.Id,
                        bienSo = x.BienSo,
                        tenLaiXe = x.TenLaiXe,
                        maChiNhanh = x.MaChiNhanh,
                        ngayDuKien = x.ChuyenHienTai.NgayDuKien,
                        hangHoas = x.ChuyenHienTai.HangHoas
                    })
                    .ToList();

                return Ok(dangVe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetDanhSachXe");
                return StatusCode(500);
            }
        }

        // ── PRIVATE HELPERS ──
        private async Task<DanhSachXeDto?> TimXeTuApi(string bienSoChuanHoa)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json) ?? new();

                return xes.FirstOrDefault(x =>
                    x.BienSo.Replace("-", "").Replace(".", "").ToUpper() == bienSoChuanHoa);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Camera] Lỗi gọi API: {msg}", ex.Message);
                return null;
            }
        }

        private static string TrangThaiLabel(int tt) => tt switch
        {
            0 => "Chưa về",
            1 => "Đang về",
            2 => "Đã đến",
            3 => "Đang nhập hàng",
            4 => "Hoàn thành",
            _ => "--"
        };
    }

    // ── REQUEST / RESPONSE MODELS ──
    public class XacNhanVaoBaiRequest
    {
        public int ChuyenId { get; set; }
        public string BienSo { get; set; } = "";
    }

    public class NhanDienRequest
    {
        public string BienSo { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string ThoiGian { get; set; } = string.Empty;
        public string Nguon { get; set; } = "webcam";
    }

    public class KetQuaNhanDien
    {
        public string BienSo { get; set; } = string.Empty;
        public bool TimThay { get; set; }
        public string LoaiPhieu { get; set; } = string.Empty;
        public int? PhieuId { get; set; }
        public string TenCua { get; set; } = string.Empty;
        public int TrangThai { get; set; }
        public DateTime ThoiGian { get; set; }
        public double Confidence { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }
}
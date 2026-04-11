using LedApp.DTOs;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    [Authorize]
    public class BaoVeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<BaoVeController> _logger;

        public BaoVeController(
            IHttpClientFactory httpClientFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<BaoVeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        // GET: /BaoVe
        // Hiển thị màn hình bảo vệ — danh sách xe DangVe (TrangThai = 1)
        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Dashboard API trả về {StatusCode}", response.StatusCode);
                    return View(new List<DanhSachXeDto>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var allXes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                             ?? new List<DanhSachXeDto>();

                // Lọc xe đang về (DangVe = 1)
                var dangVe = allXes
                    .Where(x => x.ChuyenHienTai?.TrangThai == 1)
                    .ToList();

                return View(dangVe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi load màn hình bảo vệ");
                return View(new List<DanhSachXeDto>());
            }
        }

        // POST: /BaoVe/XacNhanVaoBai
        // Bảo vệ xác nhận xe vào bãi → đổi TrangThai DangVe(1) → DaDen(2)
        [HttpPost]
        public async Task<IActionResult> XacNhanVaoBai([FromBody] XacNhanVaoBaiRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");

                // Gọi API ngoài cập nhật trạng thái → DaDen = 2
                var response = await client.PutAsJsonAsync(
                    $"api/DanhSachXes/chuyen/{request.ChuyenId}/trang-thai", 2);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Cập nhật trạng thái thất bại: chuyenId={ChuyenId}", request.ChuyenId);
                    return StatusCode((int)response.StatusCode, new { message = "Cập nhật trạng thái thất bại" });
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);

                int chuyenId = (int)result.chuyenId;
                int trangThai = (int)result.trangThai;
                string? thoiGianDen = result.thoiGianDen?.ToString();
                string? thoiGianHoanThanh = result.thoiGianHoanThanh?.ToString();

                // Broadcast SignalR → cập nhật bảng LED tổng hợp realtime
                await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated",
                    chuyenId, trangThai, thoiGianDen, thoiGianHoanThanh);

                _logger.LogInformation("Xe vào bãi OK: chuyenId={ChuyenId}, bienSo={BienSo}",
                    request.ChuyenId, request.BienSo);

                return Ok(new
                {
                    success = true,
                    message = $"Xe {request.BienSo} đã vào bãi",
                    thoiGianDen
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XacNhanVaoBai chuyenId={ChuyenId}", request.ChuyenId);
                return StatusCode(500, new { message = "Lỗi server" });
            }
        }

        // GET: /BaoVe/GetDanhSachXe
        // AJAX refresh danh sách xe DangVe (dùng cho auto-refresh ở view)
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
    }

    // Request model
    public class XacNhanVaoBaiRequest
    {
        public int ChuyenId { get; set; }
        public string BienSo { get; set; } = "";
    }
}
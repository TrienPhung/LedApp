using LedApp.Hubs;
using LedApp.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    public class ImportFromApi : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<ImportFromApi> _logger;

        public ImportFromApi(
            IHttpClientFactory httpClientFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<ImportFromApi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
        }


        // ImportFromApi.cs - thêm action này
        [HttpPost]
        public async Task<IActionResult> ForceRefresh()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                           ?? new List<DanhSachXeDto>();

                await _hubContext.Clients.All.SendAsync("ReceiveDashboard", data);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ForceRefresh");
                return StatusCode(500);
            }
        }
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int chuyenId, int trangThai)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.PutAsJsonAsync(
                    $"api/DanhSachXes/chuyen/{chuyenId}/trangthai", trangThai);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);

                // Ép kiểu rõ ràng trước khi truyền vào SendAsync
                int id = (int)result.chuyenId;
                int tt = (int)result.trangThai;
                string thoiGianDen = result.thoiGianDen?.ToString();
                string thoiGianHoanThanh = result.thoiGianHoanThanh?.ToString();

                // Phát sự kiện realtime qua SignalR
                await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated",
                    id,
                    tt,
                    thoiGianDen,
                    thoiGianHoanThanh
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CapNhatTrangThai");
                return StatusCode(500);
            }
        }




        // ✅ 1. VIEW DASHBOARD
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
                var dataList = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                               ?? new List<DanhSachXeDto>();
                // 🔥 sắp xếp theo ngày dự kiến giảm dần
                dataList = dataList
                    .OrderByDescending(x => x.ChuyenHienTai?.NgayDuKien)//sua cho nay
                    .ToList();

                return View(dataList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi dashboard API");
                return View(new List<DanhSachXeDto>());
            }
        }

        // ✅ 2. REALTIME: Lấy dashboard mới nhất + push SignalR
        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "API lỗi");

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                           ?? new List<DanhSachXeDto>();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi GetLatest");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ 3. LỊCH SỬ XE - trả JSON + push SignalR
        [HttpGet("Xe/{xeId}/ChuyenXes")]
        public async Task<IActionResult> GetChuyenXeTheoXe(int xeId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync($"api/DanhSachXes/{xeId}/lich-su");

                if (!response.IsSuccessStatusCode)
                    return NotFound($"Không tìm thấy xe {xeId}");

                var json = await response.Content.ReadAsStringAsync();
                var chuyenXes = JsonConvert.DeserializeObject<List<ChuyenXeDto>>(json)
                                ?? new List<ChuyenXeDto>();

                // 🔥 Push realtime
                await _hubContext.Clients.All.SendAsync("XeHistoryUpdated", xeId, chuyenXes);

                return Ok(chuyenXes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử xe {XeId}", xeId);
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ 4. VIEW LỊCH SỬ XE
        public async Task<IActionResult> XeHistory(int xeId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");

                // Lấy thông tin xe (để hiển thị header)
                var xeRes = await client.GetAsync($"api/DanhSachXes/{xeId}");
                if (xeRes.IsSuccessStatusCode)
                {
                    var xeJson = await xeRes.Content.ReadAsStringAsync();
                    var xe = JsonConvert.DeserializeObject<DanhSachXeDto>(xeJson);
                    ViewBag.BienSo = xe?.BienSo;
                    ViewBag.TenLaiXe = xe?.TenLaiXe;
                    ViewBag.MaChiNhanh = xe?.MaChiNhanh;
                }

                // Lấy lịch sử chuyến
                var response = await client.GetAsync($"api/DanhSachXes/{xeId}/lich-su");
                if (!response.IsSuccessStatusCode) return NotFound();

                var json = await response.Content.ReadAsStringAsync();
                var chuyenXes = JsonConvert.DeserializeObject<List<ChuyenXeDto>>(json)
                                ?? new List<ChuyenXeDto>();

                return View(chuyenXes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi load lịch sử xe {XeId}", xeId);
                return View(new List<ChuyenXeDto>());
            }
        }
        [HttpPost]
        public async Task<IActionResult> Notify()
        {
            Console.WriteLine("✅ Notify được gọi!"); // thêm dòng này
            await _hubContext.Clients.All.SendAsync("ReloadDashboard");
            return Ok();
        }
    }
}
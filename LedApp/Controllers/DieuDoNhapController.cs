using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Data;
using LedApp.DTOs;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    [Authorize(Policy = "DieuDoNhap.View")]
    public class DieuDoNhapController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<DieuDoNhapController> _logger;

        public DieuDoNhapController(
            ApplicationDBContext context,
            IHttpClientFactory httpClientFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<DieuDoNhapController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        // GET: /DieuDo
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách cửa nhập
            ViewBag.CuaNhaps = await _context.CuaNhaps.ToListAsync();
            ViewBag.UserName = User.Identity?.Name ?? "";
            ViewBag.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            // Lấy dashboard từ API Viettel
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                              ?? new List<DanhSachXeDto>();
                    ViewBag.XeList = xes;
                }
                else
                {
                    ViewBag.XeList = new List<DanhSachXeDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi load dashboard điều độ");
                ViewBag.XeList = new List<DanhSachXeDto>();
            }

            return View();
        }
        // POST: /DieuDo/PhanCong
        [HttpPost]
        public async Task<IActionResult> PhanCong([FromBody] PhanCongRequest req)
        {
            try
            {
                var cua = await _context.CuaNhaps.FindAsync(req.CuaNhapId);
                if (cua == null)
                    return BadRequest(new { message = "Cửa nhập không tồn tại" });

                var exists = await _context.Nhaps
                    .AnyAsync(n => n.BienSoXe == req.BienSo
                                && n.TrangThai != (int)TrangThaiNhap.HoanThanh);
                if (exists)
                    return BadRequest(new { message = $"Xe {req.BienSo} đã được phân công rồi" });
                // Lấy UserId người đang đăng nhập
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var nguoiDung = userId != null
                    ? await _context.nguoiDungs.FirstOrDefaultAsync(n => n.UserId == userId)
                    : null;

                var nhap = new Nhap
                {
                    CuaNhapId = req.CuaNhapId,
                    BienSoXe = req.BienSo,
                    ThoiGianPhanCong = req.ThoiGianPhanCong ?? DateTime.Now,
                    ThoiGianVaoBai = req.ThoiGianVaoBai,
                    ThoiGianGioiHan = req.ThoiGianGioiHan,
                    GhiChu = req.GhiChu,
                    NhanVienXacNhanId = nguoiDung?.Id,
                    TrangThai = (int)TrangThaiNhap.DaPhanCong
                };
                _context.Nhaps.Add(nhap);
                await _context.SaveChangesAsync();

                foreach (var h in req.HangHoas)
                {
                    _context.ChitietNhaps.Add(new ChitietNhap
                    {
                        NhapId = nhap.Id,
                        DonVi = h.DonVi,
                        ChuaBG = h.SoLuong,
                        DaBG = 0
                    });
                }
                await _context.SaveChangesAsync();

                // ✅ FIX: Tự fetch + tính toán rồi push đúng event "UpdateBangTongHop"
                await PushTongHopNhap();

                _logger.LogInformation("Phân công thành công: {BienSo} → Cửa {CuaNhapId}", req.BienSo, req.CuaNhapId);

                return Ok(new { success = true, nhapId = nhap.Id, message = $"Đã phân công xe {req.BienSo} vào {cua.Ten}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi PhanCong");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ✅ Tách riêng để tái sử dụng
        private async Task PushTongHopNhap()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!response.IsSuccessStatusCode) return;

                var json = await response.Content.ReadAsStringAsync();
                var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json) ?? new();

                int sapVeXe = 0, daVeXe = 0;
                var hangHoaMap = new Dictionary<string, (long SapVe, long DaVe)>();

                foreach (var xe in xes)
                {
                    var cx = xe.ChuyenHienTai;
                    if (cx == null) continue;

                    bool laSapVe = cx.TrangThai == 1;
                    bool laDaVe = cx.TrangThai == 2;
                    if (laSapVe) sapVeXe++;
                    if (laDaVe) daVeXe++;

                    foreach (var h in cx.HangHoas)
                    {
                        if (!hangHoaMap.ContainsKey(h.DonVi))
                            hangHoaMap[h.DonVi] = (0, 0);
                        var cur = hangHoaMap[h.DonVi];
                        hangHoaMap[h.DonVi] = (
                            cur.SapVe + (laSapVe ? h.SoLuong : 0),
                            cur.DaVe + (laDaVe ? h.SoLuong : 0)
                        );
                    }
                }
                // ✅ Lấy biển số xe đang DaPhanCong
                var bienSoChuaVao = await _context.Nhaps
                    .Where(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong)
                    .Select(n => n.BienSoXe)
                    .ToListAsync();

                int chuaVaoXe = bienSoChuaVao.Count;

                // ✅ Tính hàng hóa chưa vào cửa — gom từ xe DaPhanCong
                var chuaVaoHangMap = new Dictionary<string, long>();
                foreach (var xe in xes)
                {
                    if (!bienSoChuaVao.Contains(xe.BienSo)) continue;
                    var cx = xe.ChuyenHienTai;
                    if (cx == null) continue;
                    foreach (var h in cx.HangHoas)
                    {
                        if (!chuaVaoHangMap.ContainsKey(h.DonVi))
                            chuaVaoHangMap[h.DonVi] = 0;
                        chuaVaoHangMap[h.DonVi] += h.SoLuong;
                    }
                }

                var result = new
                {
                    SapVeXe = sapVeXe,
                    DaVeXe = daVeXe,
                    ChuaVaoXe = chuaVaoXe,
                    HangHoas = hangHoaMap.Select(kv => new
                    {
                        DonVi = kv.Key,
                        SapVe = kv.Value.SapVe,
                        DaVe = kv.Value.DaVe,
                        ChuaVao = chuaVaoHangMap.GetValueOrDefault(kv.Key, 0L) 
                    }).ToList()
                };

                await _hubContext.Clients.All.SendAsync("UpdateBangTongHop", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi PushTongHopNhap");
            }
        }
        // GET: /DieuDo/GetXeList — AJAX refresh
        [HttpGet]
        public async Task<IActionResult> GetXeList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);

                var json = await response.Content.ReadAsStringAsync();
                var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json)
                          ?? new List<DanhSachXeDto>();
                var daPhanList = await _context.Nhaps
                    .Where(n => n.TrangThai != (int)TrangThaiNhap.HoanThanh)
                    .Select(n => new { n.BienSoXe, n.CuaNhapId, NhapId = n.Id, n.TrangThai, n.ThoiGianGioiHan, n.ThoiGianPhanCong })
                    .ToListAsync();
                var bienSoDaPhan = daPhanList.Select(x => x.BienSoXe).ToList();

                var result = xes.Select(x => new
                {
                    id = x.Id,
                    bienSo = x.BienSo,
                    tenLaiXe = x.TenLaiXe,
                    maChiNhanh = x.MaChiNhanh,
                    chuyenHienTai = x.ChuyenHienTai == null ? null : new
                    {
                        id = x.ChuyenHienTai.Id,
                        trangThai = x.ChuyenHienTai.TrangThai,
                        ngayDuKien = x.ChuyenHienTai.NgayDuKien,
                        maChuyenApi = x.ChuyenHienTai.MaChuyenApi,
                        lanThu = x.ChuyenHienTai.LanThu,
                        hangHoas = x.ChuyenHienTai.HangHoas,
                        thoiGianDen = x.ChuyenHienTai.ThoiGianDen
                    },
                    daPhanCong = bienSoDaPhan.Contains(x.BienSo),
                    cuaNhapId = daPhanList.FirstOrDefault(d => d.BienSoXe == x.BienSo)?.CuaNhapId,
                    nhapId = daPhanList.FirstOrDefault(d => d.BienSoXe == x.BienSo)?.NhapId,  // ✅ thêm
                    trangThaiNhap = daPhanList.FirstOrDefault(d => d.BienSoXe == x.BienSo)?.TrangThai,  // ← thêm dòng này
                    thoiGianGioiHan = daPhanList.FirstOrDefault(d => d.BienSoXe == x.BienSo)?.ThoiGianGioiHan,  // ← thêm
                    thoiGianPhanCong = daPhanList.FirstOrDefault(d => d.BienSoXe == x.BienSo)?.ThoiGianPhanCong // ✅ THÊM

                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetXeList");
                return StatusCode(500);
            }
        }
        // POST: /DieuDo/HuyPhanCong
        [HttpPost]
        public async Task<IActionResult> HuyPhanCong([FromBody] HuyPhanCongRequest req)
        {
            try
            {
                var nhap = await _context.Nhaps
                    .Include(n => n.ChitietNhaps)
                    .FirstOrDefaultAsync(n => n.Id == req.NhapId);

                if (nhap == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                // Chỉ cho huỷ khi chưa vào cửa
                if (nhap.TrangThai != (int)TrangThaiNhap.DaPhanCong)
                    return BadRequest(new { message = "Xe đã vào cửa, không thể huỷ" });

                // Xoá ChitietNhap trước
                _context.ChitietNhaps.RemoveRange(nhap.ChitietNhaps ?? new List<ChitietNhap>());
                // Xoá Nhap
                _context.Nhaps.Remove(nhap);
                await _context.SaveChangesAsync();

                // SignalR → cập nhật panel cửa về trống
                // ✅ Mới — push cả 2: reset màn hình nhân viên + cập nhật bảng tổng hợp
                var cuaNhapId = nhap.CuaNhapId;
                await _hubContext.Clients.All.SendAsync("ReceivedNhap", (object?)null, cuaNhapId);
                await _hubContext.Clients.All.SendAsync("ReceivedChitietNhap", (object?)null, cuaNhapId);
                await PushTongHopNhap();

                _logger.LogInformation("Huỷ phân công nhapId={Id} bienSo={BienSo}", nhap.Id, nhap.BienSoXe);

                return Ok(new { success = true, message = $"Đã huỷ phân công xe {nhap.BienSoXe}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi HuyPhanCong nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message });
            }
        }
        // GET: /DieuDo/GetCanhBao
        [HttpGet]
        public async Task<IActionResult> GetCanhBao()
        {
            try
            {
                var list = await _context.CanhBaos
                    .OrderByDescending(c => c.ThoiGian)
                    .Take(50)
                    .Select(c => new
                    {
                        id = c.Id,
                        loaiCanhBao = c.LoaiCanhBao,
                        phieuId = c.PhieuId,
                        thoiGian = c.ThoiGian,
                        ghiChu = c.GhiChu,
                        trangThai = c.TrangThai
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetCanhBao");
                return StatusCode(500);
            }
        }

        // POST: /DieuDo/CapNhatTrangThaiCanhBao
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThaiCanhBao([FromBody] CapNhatCanhBaoRequest req)
        {
            try
            {
                var cb = await _context.CanhBaos.FindAsync(req.CanhBaoId);
                if (cb == null)
                    return NotFound(new { message = "Không tìm thấy cảnh báo" });

                cb.TrangThai = req.TrangThai;
                await _context.SaveChangesAsync();

                // SignalR → cập nhật badge chuông cho tất cả điều độ
                var chuaXuLy = await _context.CanhBaos
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CapNhatTrangThaiCanhBao");
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetChitietCua()
        {
            var chitiets = await _context.Nhaps
                .Where(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao
                         || n.TrangThai == (int)TrangThaiNhap.QuaThoiGian)
                .Include(n => n.ChitietNhaps)
                .Select(n => new
                {
                    cuaNhapId = n.CuaNhapId,
                    chitiet = n.ChitietNhaps.Select(c => new
                    {
                        donVi = c.DonVi,
                        chuaBG = c.ChuaBG,
                        daBG = c.DaBG
                    }).ToList()
                })
                .ToListAsync();

            return Ok(chitiets);
        }


    }

    public class CapNhatCanhBaoRequest
    {
        public int CanhBaoId { get; set; }
        public int TrangThai { get; set; }
    }
    public class HuyPhanCongRequest
    {
        public int NhapId { get; set; }
    }
    // Request model
    public class PhanCongRequest
    {
        public string BienSo { get; set; } = "";
        public int CuaNhapId { get; set; }
        public DateTime? ThoiGianPhanCong { get; set; }
        public DateTime? ThoiGianVaoBai { get; set; }  
        public DateTime? ThoiGianGioiHan { get; set; }
        public string? GhiChu { get; set; }
        public List<HangHoaItemDto> HangHoas { get; set; } = new();
    }
}
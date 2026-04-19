using System.Net.Http;
using LedApp.Data;
using LedApp.DTOs;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LedApp.Controllers
{
    [Authorize]
    public class NhanVienNhapController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<NhanVienNhapController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _connectionString;

        public NhanVienNhapController(
            ApplicationDBContext context,
            IHubContext<SignalServer> hubContext,
            ILogger<NhanVienNhapController> logger,
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IActionResult> CuaNhap(int cuaNhapId)
        {
            var cua = await _context.CuaNhaps.FindAsync(cuaNhapId);
            if (cua == null) return NotFound();
            ViewBag.CuaNhapId = cuaNhapId;
            ViewBag.TenCua = cua.Ten;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNhapHienTai(int cuaNhapId)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
                optionsBuilder.UseSqlServer(_connectionString);
                await using var db = new ApplicationDBContext(optionsBuilder.Options);

                var nhap = await db.Nhaps
                    .AsNoTracking()
                    .Include(n => n.ChitietNhaps)
                    .Where(n => n.CuaNhapId == cuaNhapId
                             && n.TrangThai != (int)TrangThaiNhap.HoanThanh)
                    .OrderByDescending(n => n.ThoiGianPhanCong)
                    .FirstOrDefaultAsync();

                _logger.LogInformation(
                    "GetNhapHienTai cuaNhapId={Id} → nhap={NhapId} trangThai={TT}",
                    cuaNhapId,
                    nhap?.Id.ToString() ?? "null",
                    nhap?.TrangThai.ToString() ?? "null");

                if (nhap == null) return Ok(null);

                return Ok(new
                {
                    nhapId = nhap.Id,
                    bienSoXe = nhap.BienSoXe,
                    trangThai = nhap.TrangThai,
                    thoiGianVaoBai = nhap.ThoiGianVaoBai,
                    thoiGianVaoCua = nhap.ThoiGianVaoCua,
                    thoiGianGioiHan = nhap.ThoiGianGioiHan,
                    ghiChu = nhap.GhiChu,
                    chitiet = (nhap.ChitietNhaps ?? new List<ChitietNhap>())
                        .Select(c => new { id = c.Id, donVi = c.DonVi, chuaBG = c.ChuaBG, daBG = c.DaBG })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetNhapHienTai cuaNhapId={Id}", cuaNhapId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> XacNhanVaoCua([FromBody] XacNhanVaoCuaRequest req)
        {
            try
            {
                var nhap = await _context.Nhaps
                    .AsTracking()
                    .FirstOrDefaultAsync(n => n.Id == req.NhapId);

                if (nhap == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                if (nhap.TrangThai != (int)TrangThaiNhap.DaPhanCong)
                    return BadRequest(new { message = "Phiếu nhập không ở trạng thái chờ" });

                int phutGioiHan = 40;
                var cauHinh = await _context.CauHinhs
                    .FirstOrDefaultAsync(c => c.Key == "ThoiGianNhapToiDa");
                if (cauHinh != null && int.TryParse(cauHinh.Value, out var phut))
                    phutGioiHan = phut;

                nhap.ThoiGianVaoCua = DateTime.Now;
                nhap.TrangThai = (int)TrangThaiNhap.DangBanGiao;
                nhap.ThoiGianGioiHan = DateTime.Now.AddMinutes(phutGioiHan);

                await _context.SaveChangesAsync();

                await CapNhatTrangThaiChuyenViettel(nhap.BienSoXe, 3);

                await _hubContext.Clients.All.SendAsync("ReceivedNhap", new
                {
                    bienSoXe = nhap.BienSoXe,
                    thoiGianVaoCua = nhap.ThoiGianVaoCua,
                    thoiGianGioiHan = nhap.ThoiGianGioiHan,
                    trangThai = nhap.TrangThai
                }, nhap.CuaNhapId);

                await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated", 0, 3, (string?)null, (string?)null);

                return Ok(new
                {
                    success = true,
                    thoiGianVaoCua = nhap.ThoiGianVaoCua,
                    thoiGianGioiHan = nhap.ThoiGianGioiHan,
                    phutGioiHan = phutGioiHan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XacNhanVaoCua nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BanGiao([FromBody] BanGiaoRequest req)
        {
            try
            {
                var nhap = await _context.Nhaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == req.NhapId);

                if (nhap == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                if (nhap.TrangThai != (int)TrangThaiNhap.DangBanGiao
                    && nhap.TrangThai != (int)TrangThaiNhap.QuaThoiGian)
                    return BadRequest(new { message = "Xe chưa vào cửa" });

                var chitiets = await _context.ChitietNhaps
                    .AsNoTracking()
                    .Where(c => c.NhapId == req.NhapId)
                    .ToListAsync();

                var username = User.Identity?.Name;
                int? nhanVienId = null;
                if (!string.IsNullOrEmpty(username))
                {
                    //var nv = await _context.nguoiDungs
                    //    .AsNoTracking()
                    //    .FirstOrDefaultAsync(n => n.Username == username);
                    //nhanVienId = nv?.Id;
                }

                var now = DateTime.Now;
                bool coCapNhat = false;

                foreach (var item in req.Items)
                {
                    var ct = chitiets.FirstOrDefault(c => c.Id == item.ChitietId);
                    if (ct == null) continue;

                    var soLuongBG = Math.Min(item.SoLuong, ct.ChuaBG);
                    if (soLuongBG <= 0) continue;

                    await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE ChitietNhap 
                          SET DaBG = DaBG + {0}, ChuaBG = ChuaBG - {0}
                          WHERE Id = {1} AND ChuaBG >= {0}",
                        soLuongBG, ct.Id
                    );

                    _context.LichSuBanGiaos.Add(new LichSuBanGiao
                    {
                        LoaiPhieu = "Nhap",
                        PhieuId = nhap.Id,
                        ChiTietDonViId = ct.Id,
                        DonVi = ct.DonVi,
                        SoBG = soLuongBG,
                        NhanVienId = nhanVienId,
                        ThoiGian = now
                    });

                    coCapNhat = true;
                }

                if (!coCapNhat)
                    return BadRequest(new { message = "Không có dữ liệu hợp lệ để bàn giao" });

                await _context.SaveChangesAsync();

                var chitietMoi = await _context.ChitietNhaps
                    .AsNoTracking()
                    .Where(c => c.NhapId == req.NhapId)
                    .Select(c => new { id = c.Id, donVi = c.DonVi, chuaBG = c.ChuaBG, daBG = c.DaBG })
                    .ToListAsync();

                await _hubContext.Clients.All.SendAsync("ReceivedChitietNhap", chitietMoi, nhap.CuaNhapId);

                _logger.LogInformation("Bàn giao nhapId={Id}", req.NhapId);

                return Ok(new { success = true, chitiet = chitietMoi });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BanGiao nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, inner2 = ex.InnerException?.InnerException?.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> HoanThanh([FromBody] HoanThanhRequest req)
        {
            try
            {
                var now = DateTime.Now;

                var username = User.Identity?.Name;
                int? nhanVienId = null;
                if (!string.IsNullOrEmpty(username))
                {
                    //var nv = await _context.nguoiDungs
                    //    .AsNoTracking()
                    //    .FirstOrDefaultAsync(n => n.Username == username);
                    //nhanVienId = nv?.Id;
                }

                var chitiets = await _context.ChitietNhaps
                    .AsNoTracking()
                    .Where(c => c.NhapId == req.NhapId)
                    .ToListAsync();

                var conHang = chitiets.Any(c => c.ChuaBG > 0);
                if (conHang && !req.BuocQua)
                    return BadRequest(new { message = "Vẫn còn hàng chưa bàn giao. Xác nhận bỏ qua?" });

                var nhapInfo = await _context.Nhaps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == req.NhapId);

                if (nhapInfo == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                // ✅ Tách 2 câu SQL tránh DBNull
                int rows;
                if (nhanVienId.HasValue)
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE Nhap SET 
                            TrangThai = {0}, 
                            ThoiGianHoanThanh = {1},
                            NhanVienXacNhanId = {2}
                          WHERE Id = {3}",
                        (int)TrangThaiNhap.HoanThanh,
                        now,
                        nhanVienId.Value,
                        req.NhapId
                    );
                }
                else
                {
                    rows = await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE Nhap SET 
                            TrangThai = {0}, 
                            ThoiGianHoanThanh = {1}
                          WHERE Id = {2}",
                        (int)TrangThaiNhap.HoanThanh,
                        now,
                        req.NhapId
                    );
                }

                if (rows == 0)
                    return StatusCode(500, new { message = "Không cập nhật được DB" });

                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE CanhBao 
                      SET TrangThai = {0}
                      WHERE PhieuId = {1} 
                        AND LoaiPhieu = 'NHAP'
                        AND TrangThai != {0}",
                    (int)TrangThaiCanhBao.DaXuLy,
                    req.NhapId
                );

                var chuaXuLy = await _context.CanhBaos
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                var bienSo = nhapInfo.BienSoXe;
                var cuaNhapId = nhapInfo.CuaNhapId;

                _ = Task.Run(() => CapNhatTrangThaiChuyenViettel(bienSo, 4));

                await _hubContext.Clients.All.SendAsync("ReceivedNhap", null, cuaNhapId);
                await _hubContext.Clients.All.SendAsync("ReceivedChitietNhap", null, cuaNhapId);
                await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated", 0, 4, null, null);

                _logger.LogInformation("Hoàn thành nhập nhapId={Id} bienSo={BienSo}", req.NhapId, bienSo);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi HoanThanh nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BaoSaiXe([FromBody] BaoSaiXeRequest req)
        {
            try
            {
                var nhap = await _context.Nhaps.FindAsync(req.NhapId);
                if (nhap == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                var canhBao = new CanhBao
                {
                    LoaiPhieu = "NHAP",
                    PhieuId = nhap.Id,
                    LoaiCanhBao = "BaoSaiXe",
                    ThoiGian = DateTime.Now,
                    GhiChu = $"DB:{nhap.BienSoXe}|ThucTe:{req.BienSoThucTe}|Cua:{req.CuaNhapId}",
                    TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                };
                _context.CanhBaos.Add(canhBao);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("BaoSaiXe", new
                {
                    canhBaoId = canhBao.Id,
                    nhapId = nhap.Id,
                    bienSoDuKien = nhap.BienSoXe,
                    bienSoThucTe = req.BienSoThucTe,
                    cuaNhapId = req.CuaNhapId,
                    thoiGian = canhBao.ThoiGian,
                    trangThai = canhBao.TrangThai
                });

                _logger.LogWarning("Báo sai xe: Cửa {CuaId} chờ {DuKien} nhưng thực tế là {ThucTe}",
                    req.CuaNhapId, nhap.BienSoXe, req.BienSoThucTe);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BaoSaiXe");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, inner2 = ex.InnerException?.InnerException?.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BaoCanhBaoQuaGio([FromBody] BaoCanhBaoQuaGioRequest req)
        {
            try
            {
                var nhap = await _context.Nhaps.FindAsync(req.NhapId);
                if (nhap == null)
                    return NotFound(new { message = "Không tìm thấy phiếu nhập" });

                if (nhap.TrangThai != (int)TrangThaiNhap.DangBanGiao)
                    return Ok(new { success = true, message = "Đã xử lý trước đó" });

                nhap.TrangThai = (int)TrangThaiNhap.QuaThoiGian;

                var daCoCanh = await _context.CanhBaos
                    .AnyAsync(c => c.PhieuId == nhap.Id
                                && c.LoaiCanhBao == "QuaHanNhap"
                                && c.LoaiPhieu == "NHAP");

                if (!daCoCanh)
                {
                    _context.CanhBaos.Add(new CanhBao
                    {
                        LoaiPhieu = "NHAP",
                        PhieuId = nhap.Id,
                        LoaiCanhBao = "QuaHanNhap",
                        ThoiGian = DateTime.Now,
                        GhiChu = $"Xe:{nhap.BienSoXe}|Cua:{nhap.CuaNhapId}|GioiHan:{nhap.ThoiGianGioiHan:HH:mm}",
                        TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                    });
                }

                await _context.SaveChangesAsync();

                var chuaXuLy = await _context.CanhBaos
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                await _hubContext.Clients.All.SendAsync("ReceivedNhap", new
                {
                    bienSoXe = nhap.BienSoXe,
                    thoiGianVaoCua = nhap.ThoiGianVaoCua,
                    thoiGianGioiHan = nhap.ThoiGianGioiHan,
                    trangThai = nhap.TrangThai
                }, nhap.CuaNhapId);

                _logger.LogWarning("QuaHanNhap (client báo): nhapId={Id} bienSo={BienSo} cuaNhapId={CuaNhapId}",
                    nhap.Id, nhap.BienSoXe, nhap.CuaNhapId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BaoCanhBaoQuaGio nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> XuLyCanhBao([FromBody] XuLyCanhBaoRequest req)
        {
            try
            {
                int rows = await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE TOP(1) CanhBao 
                      SET TrangThai = {0}
                      WHERE PhieuId = {1} 
                        AND LoaiPhieu = 'NHAP' 
                        AND LoaiCanhBao = 'QuaHanNhap'
                        AND TrangThai = {2}",
                    (int)TrangThaiCanhBao.DangXuLy,
                    req.NhapId,
                    (int)TrangThaiCanhBao.ChuaXuLy
                );

                if (rows == 0)
                    return Ok(new { success = true, message = "Không có cảnh báo cần xử lý" });

                var chuaXuLy = await _context.CanhBaos
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XuLyCanhBao nhapId={Id}", req.NhapId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        private async Task CapNhatTrangThaiChuyenViettel(string bienSo, int trangThai)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var resp = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!resp.IsSuccessStatusCode) return;

                var json = await resp.Content.ReadAsStringAsync();
                var xes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LedApp.DTOs.DanhSachXeDto>>(json);
                var xe = xes?.FirstOrDefault(x => x.BienSo == bienSo);
                var chuyenId = xe?.ChuyenHienTai?.Id;
                if (chuyenId == null) return;

                var content = new StringContent(trangThai.ToString(), System.Text.Encoding.UTF8, "application/json");
                await client.PutAsync($"api/DanhSachXes/chuyen/{chuyenId}/trang-thai", content);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không cập nhật được trạng thái chuyến Viettel cho xe {BienSo}", bienSo);
            }
        }
    }

    public class XuLyCanhBaoRequest { public int NhapId { get; set; } }
    public class BaoCanhBaoQuaGioRequest { public int NhapId { get; set; } }
    public class BaoSaiXeRequest { public int NhapId { get; set; } public string BienSoThucTe { get; set; } = ""; public int CuaNhapId { get; set; } }
    public class XacNhanVaoCuaRequest { public int NhapId { get; set; } }
    public class BanGiaoRequest { public int NhapId { get; set; } public List<BanGiaoItem> Items { get; set; } = new(); }
    public class BanGiaoItem { public int ChitietId { get; set; } public long SoLuong { get; set; } }
    public class HoanThanhRequest { public int NhapId { get; set; } public bool BuocQua { get; set; } = false; }
}
using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    [Authorize(Roles = "NhanVien")]
    public class NhanVienXuatController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<NhanVienXuatController> _logger;
        private readonly string _connectionString;


        public NhanVienXuatController(
            ApplicationDBContext context,
            IHubContext<SignalServer> hubContext,
            ILogger<NhanVienXuatController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        // GET: /NhanVienXuat/CuaXuat?cuaXuatId=x
        public async Task<IActionResult> CuaXuat(int cuaXuatId)
        {
            var cua = await _context.CuaXuats.FindAsync(cuaXuatId);
            if (cua == null) return NotFound();
            ViewBag.CuaXuatId = cuaXuatId;
            ViewBag.TenCua = cua.Ten;

            // Claims — không cần inject thêm gì
            ViewBag.UserName = User.Identity?.Name ?? "";
            ViewBag.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            return View();
        }

        // GET: /NhanVienXuat/GetXuatHienTai?cuaXuatId=x
        [HttpGet]
        public async Task<IActionResult> GetXuatHienTai(int cuaXuatId)
        {
            try
            {
                // Fresh DbContext — tránh stale cache
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
                optionsBuilder.UseSqlServer(_connectionString);
                await using var db = new ApplicationDBContext(optionsBuilder.Options);

                var xuat = await db.Xuats
                    .AsNoTracking()
                    .Include(x => x.Xe).ThenInclude(xe => xe != null ? xe.TaiXe : null)
                    .Include(x => x.ChitietXuats)
                    .Where(x => x.CuaXuatId == cuaXuatId
                         && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat
                         && x.TrangThai != (int)TrangThaiXuat.HoanThanh)
                    .OrderByDescending(x => x.ThoiGianPhanCong)
                    .FirstOrDefaultAsync();

                if (xuat == null) return Ok((object?)null);

                return Ok(new
                {
                    xuatId = xuat.Id,
                    bienSoXe = xuat.Xe?.BienSoXe ?? "--",
                    tenTaiXe = xuat.Xe?.TaiXe?.FullName ?? "--",
                    trangThai = xuat.TrangThai,
                    thoiGianVaoCua = xuat.ThoiGianVaoCua,
                    thoiGianGioiHan = xuat.ThoiGianGioiHan,
                    ghiChu = xuat.GhiChu,
                    chitiet = (xuat.ChitietXuats ?? new List<ChitietXuat>())
                        .Select(c => new { id = c.Id, donVi = c.DonVi, chuaBG = c.ChuaBG, daBG = c.DaBG })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetXuatHienTai cuaXuatId={Id}", cuaXuatId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // POST: /NhanVienXuat/XacNhanVaoCua
        [HttpPost]
        public async Task<IActionResult> XacNhanVaoCua([FromBody] XuatVaoCuaRequest req)
        {
            try
            {
                var xuat = await _context.Xuats
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai != (int)TrangThaiXuat.DaPhanCong)
                    return BadRequest(new { message = "Phiếu không ở trạng thái chờ xe vào!" });

                var cauHinh = await _context.CauHinhs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Key == "ThoiGianXuatToiDa");
                int phutToiDa = 45;
                if (cauHinh != null && int.TryParse(cauHinh.Value, out int parsed))
                    phutToiDa = parsed;

                var now = DateTime.Now;
                xuat.ThoiGianVaoCua = now;
                xuat.ThoiGianGioiHan = now.AddMinutes(phutToiDa);
                xuat.TrangThai = (int)TrangThaiXuat.DangBanGiao;

                await _context.SaveChangesAsync();

                // SignalR
                await _hubContext.Clients.All.SendAsync("ReceivedXuat", new
                {
                    bienSoXe = xuat.Xe?.BienSoXe,
                    thoiGianVaoCua = xuat.ThoiGianVaoCua,
                    thoiGianGioiHan = xuat.ThoiGianGioiHan,
                    trangThai = xuat.TrangThai
                }, xuat.CuaXuatId);
                await PushXuatUpdate();

                return Ok(new
                {
                    success = true,
                    thoiGianVaoCua = xuat.ThoiGianVaoCua,
                    thoiGianGioiHan = xuat.ThoiGianGioiHan,
                    phutToiDa
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XacNhanVaoCua xuatId={Id}", req.XuatId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // POST: /NhanVienXuat/BanGiao
        [HttpPost]
        public async Task<IActionResult> BanGiao([FromBody] BanGiaoXuatRequest req)
        {
            try
            {
                // Lấy xuat — AsNoTracking để tránh conflict
                var xuat = await _context.Xuats
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai != (int)TrangThaiXuat.DangBanGiao
                 && xuat.TrangThai != (int)TrangThaiXuat.QuaThoiGian)
                    return BadRequest(new { message = "Phiếu không ở trạng thái bàn giao!" });

                var chitiets = await _context.ChitietXuats
                    .AsNoTracking()
                    .Where(c => c.XuatId == req.XuatId)
                    .ToListAsync();

                var userId = GetCurrentUserId();
                var now = DateTime.Now;
                bool coCapNhat = false;

                foreach (var item in req.Items)
                {
                    var ct = chitiets.FirstOrDefault(c => c.Id == item.ChitietId);
                    if (ct == null) continue;
                    var soLuongBG = Math.Min(item.SoLuong, ct.ChuaBG);
                    if (soLuongBG <= 0) continue;

                    // ✅ Raw SQL — tránh tracking conflict
                    await _context.Database.ExecuteSqlRawAsync(
                        @"UPDATE ChitietXuat 
                          SET DaBG = DaBG + {0}, ChuaBG = ChuaBG - {0}
                          WHERE Id = {1} AND ChuaBG >= {0}",
                        soLuongBG, ct.Id
                    );

                    _context.LichSuBanGiaos.Add(new LichSuBanGiao
                    {
                        LoaiPhieu = "XUAT",
                        PhieuId = req.XuatId,
                        ChiTietDonViId = ct.Id,
                        DonVi = ct.DonVi,
                        SoBG = soLuongBG,
                        NhanVienId = userId,
                        ThoiGian = now
                    });
                    coCapNhat = true;
                }

                if (!coCapNhat)
                    return BadRequest(new { message = "Không có dữ liệu hợp lệ để bàn giao" });

                await _context.SaveChangesAsync();

                // Lấy chitiet mới nhất trả về client
                var chitietMoi = await _context.ChitietXuats
                    .AsNoTracking()
                    .Where(c => c.XuatId == req.XuatId)
                    .Select(c => new { id = c.Id, donVi = c.DonVi, chuaBG = c.ChuaBG, daBG = c.DaBG })
                    .ToListAsync();

                // SignalR → cập nhật real-time
                await _hubContext.Clients.All.SendAsync("ReceivedChitietXuat", chitietMoi, xuat.CuaXuatId);
                await PushXuatUpdate();

                _logger.LogInformation("Bàn giao xuất xuatId={Id}", req.XuatId);
                return Ok(new { success = true, chitiet = chitietMoi });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BanGiao xuatId={Id}", req.XuatId);
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    inner2 = ex.InnerException?.InnerException?.Message
                });
            }
        }

        // POST: /NhanVienXuat/HoanThanh
        [HttpPost]
        public async Task<IActionResult> HoanThanh([FromBody] HoanThanhXuatRequest req)
        {
            try
            {
                var chitiets = await _context.ChitietXuats
                    .AsNoTracking()
                    .Where(c => c.XuatId == req.XuatId)
                    .ToListAsync();

                var conHang = chitiets.Any(c => c.ChuaBG > 0);
                if (conHang && !req.BuocQua)
                    return BadRequest(new { message = "Vẫn còn hàng chưa bàn giao. Xác nhận bỏ qua?" });

                var xuatInfo = await _context.Xuats
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);
                if (xuatInfo == null)
                    return NotFound(new { message = "Không tìm thấy phiếu xuất" });

                var now = DateTime.Now;

                // ✅ Raw SQL UPDATE
                int rows = await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE Xuat SET 
                        TrangThai = {0},
                        ThoiGianHoanThanh = {1}
                      WHERE Id = {2}",
                    (int)TrangThaiXuat.HoanThanh,
                    now,
                    req.XuatId
                );

                if (rows == 0)
                    return StatusCode(500, new { message = "Không cập nhật được DB" });

                // Đóng hết CanhBao liên quan
                await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE CanhBao 
                      SET TrangThai = {0}
                      WHERE PhieuId = {1} 
                        AND LoaiPhieu = 'XUAT'
                        AND TrangThai != {0}",
                    (int)TrangThaiCanhBao.DaXuLy,
                    req.XuatId
                );

                var chuaXuLy = await _context.CanhBaos
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                // SignalR
                await _hubContext.Clients.All.SendAsync("ReceivedXuat", null, xuatInfo.CuaXuatId);
                await _hubContext.Clients.All.SendAsync("ReceivedChitietXuat", null, xuatInfo.CuaXuatId);
                await PushXuatUpdate();

                _logger.LogInformation("Hoàn thành xuất xuatId={Id}", req.XuatId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi HoanThanh xuatId={Id}", req.XuatId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // POST: /NhanVienXuat/BaoCanhBaoQuaGio
        [HttpPost]
        public async Task<IActionResult> BaoCanhBaoQuaGio([FromBody] XuatIdRequest req)
        {
            try
            {
                // ← THÊM AsTracking() rõ ràng và reload fresh
                var xuat = await _context.Xuats
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);  // ← đổi FindAsync → FirstOrDefaultAsync

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });

                if (xuat.TrangThai == (int)TrangThaiXuat.DangBanGiao)
                {
                    xuat.TrangThai = (int)TrangThaiXuat.QuaThoiGian;

                    var daCoCanh = await _context.CanhBaos
                        .AnyAsync(c => c.PhieuId == xuat.Id
                                    && c.LoaiCanhBao == "QuaHanXuat"
                                    && c.LoaiPhieu == "XUAT");

                    if (!daCoCanh)
                    {
                        _context.CanhBaos.Add(new CanhBao
                        {
                            LoaiPhieu = "XUAT",
                            PhieuId = xuat.Id,
                            LoaiCanhBao = "QuaHanXuat",
                            ThoiGian = DateTime.Now,
                            GhiChu = $"Xe:{xuat.XeId}|Cua:{xuat.CuaXuatId}|GioiHan:{xuat.ThoiGianGioiHan:HH:mm}",
                            // ← dùng XeId thay vì Xe?.BienSoXe để tránh null ref
                            TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                        });
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Background service đã update trước — reload và tiếp tục
                        _logger.LogWarning("Concurrency conflict BaoCanhBaoQuaGio xuatId={Id}, bỏ qua", req.XuatId);
                    }

                    var chuaXuLy = await _context.CanhBaos
                        .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                    await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);
                }

                // LUÔN push SignalR dù có update hay không
                await _hubContext.Clients.All.SendAsync("ReceivedXuat", new
                {
                    bienSoXe = xuat.XeId?.ToString(),
                    thoiGianVaoCua = xuat.ThoiGianVaoCua,
                    thoiGianGioiHan = xuat.ThoiGianGioiHan,
                    trangThai = xuat.TrangThai
                }, xuat.CuaXuatId);
                await PushXuatUpdate();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BaoCanhBaoQuaGio xuatId={Id}", req.XuatId);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: /NhanVienXuat/XuLyCanhBao
        [HttpPost]
        public async Task<IActionResult> XuLyCanhBao([FromBody] XuatIdRequest req)
        {
            try
            {
                int rows = await _context.Database.ExecuteSqlRawAsync(
                    @"UPDATE TOP(1) CanhBao 
                      SET TrangThai = {0}
                      WHERE PhieuId = {1} 
                        AND LoaiPhieu = 'XUAT' 
                        AND LoaiCanhBao = 'QuaHanXuat'
                        AND TrangThai = {2}",
                    (int)TrangThaiCanhBao.DangXuLy,
                    req.XuatId,
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
                _logger.LogError(ex, "Lỗi XuLyCanhBao xuatId={Id}", req.XuatId);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // POST: /NhanVienXuat/BaoSaiXe
        [HttpPost]
        public async Task<IActionResult> BaoSaiXe([FromBody] BaoSaiXeXuatRequest req)
        {
            try
            {
                var xuat = await _context.Xuats
                    .AsNoTracking()
                    .Include(x => x.Xe)
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);
                if (xuat == null)
                    return NotFound(new { message = "Không tìm thấy phiếu xuất" });

                var canhBao = new CanhBao
                {
                    LoaiPhieu = "XUAT",
                    PhieuId = xuat.Id,
                    LoaiCanhBao = "BaoSaiXe",
                    ThoiGian = DateTime.Now,
                    GhiChu = $"DB:{xuat.Xe?.BienSoXe}|ThucTe:{req.BienSoThucTe}|Cua:{req.CuaXuatId}",
                    TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                };
                _context.CanhBaos.Add(canhBao);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("BaoSaiXeXuat", new
                {
                    canhBaoId = canhBao.Id,
                    xuatId = xuat.Id,
                    bienSoDuKien = xuat.Xe?.BienSoXe,
                    bienSoThucTe = req.BienSoThucTe,
                    cuaXuatId = req.CuaXuatId,
                    thoiGian = canhBao.ThoiGian,
                    trangThai = canhBao.TrangThai
                });

                _logger.LogWarning("BaoSaiXe xuất: Cửa {CuaId} chờ {DuKien} nhưng thực tế là {ThucTe}",
                    req.CuaXuatId, xuat.Xe?.BienSoXe, req.BienSoThucTe);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi BaoSaiXe xuất");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ── Helper: push SignalR tổng hợp xuất ──
        private async Task PushXuatUpdate()
        {
            var today = DateTime.Today;
            var tatCa = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today)
                .Include(x => x.ChitietXuats)
                .ToListAsync();

            var choXuat = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DaPhanCong).ToList();
            var dangXuat = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao
                                         || x.TrangThai == (int)TrangThaiXuat.QuaThoiGian
                                         || x.TrangThai == (int)TrangThaiXuat.HoanThanh).ToList();
            var daRoiKho = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DaXuatPhat).ToList();

            var hangHoaMap = new Dictionary<string, (long ChoXuat, long DangXuat, long DaRoi)>();
            void AddHang(List<Xuat> list, string cot)
            {
                foreach (var x in list)
                    foreach (var c in x.ChitietXuats ?? new List<ChitietXuat>())
                    {
                        if (string.IsNullOrEmpty(c.DonVi)) continue;
                        if (!hangHoaMap.ContainsKey(c.DonVi))
                            hangHoaMap[c.DonVi] = (0, 0, 0);
                        var cur = hangHoaMap[c.DonVi];
                        hangHoaMap[c.DonVi] = cot switch
                        {
                            "cho" => (cur.ChoXuat + c.ChuaBG, cur.DangXuat, cur.DaRoi),
                            "dang" => (cur.ChoXuat, cur.DangXuat + c.ChuaBG, cur.DaRoi),
                            _ => (cur.ChoXuat, cur.DangXuat, cur.DaRoi + c.DaBG)
                        };
                    }
            }
            AddHang(choXuat, "cho");
            AddHang(dangXuat, "dang");
            AddHang(daRoiKho, "da");

            await _hubContext.Clients.All.SendAsync("UpdateBangTongHopXuat", new
            {
                ChoXuatXe = choXuat.Count,
                DangXuatXe = dangXuat.Count,
                DaRoiKhoXe = daRoiKho.Count,
                HangHoas = hangHoaMap.Select(kv => new
                {
                    DonVi = kv.Key,
                    ChoXuat = kv.Value.ChoXuat,
                    DangXuat = kv.Value.DangXuat,
                    DaRoi = kv.Value.DaRoi
                }).ToList()
            });
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null) return null;
            return int.TryParse(claim.Value, out int id) ? id : null;
        }
    }

    // ── Request DTOs ──
    public class XuatIdRequest { public int XuatId { get; set; } }
    public class XuatVaoCuaRequest { public int XuatId { get; set; } }
    public class HoanThanhXuatRequest { public int XuatId { get; set; } public bool BuocQua { get; set; } = false; }

    public class BanGiaoXuatRequest
    {
        public int XuatId { get; set; }
        public List<BanGiaoXuatItem> Items { get; set; } = new();
    }
    public class BanGiaoXuatItem
    {
        public int ChitietId { get; set; }
        public string DonVi { get; set; } = "";
        public long SoLuong { get; set; }
    }

    public class BaoSaiXeXuatRequest
    {
        public int XuatId { get; set; }
        public string BienSoThucTe { get; set; } = "";
        public int CuaXuatId { get; set; }
    }
}
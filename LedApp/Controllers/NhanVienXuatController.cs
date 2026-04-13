using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    public class NhanVienXuatController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;

        public NhanVienXuatController(ApplicationDBContext context, IHubContext<SignalServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: /NhanVienXuat/{id}
        public async Task<IActionResult> Index(int id)
        {
            var cua = await _context.CuaXuats.FindAsync(id);
            if (cua == null) return NotFound();
            ViewBag.CuaId = id;
            ViewBag.TenCua = cua.Ten;
            return View();
        }

        // POST: /NhanVienXuat/XacNhanVaoCua
        [HttpPost]
        public async Task<IActionResult> XacNhanVaoCua([FromBody] XuatIdRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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
                    .FirstOrDefaultAsync(c => c.Key == "ThoiGianXuatToiDa");
                int phutToiDa = 45;
                if (cauHinh != null && int.TryParse(cauHinh.Value, out int parsed))
                    phutToiDa = parsed;

                var now = DateTime.Now;
                xuat.ThoiGianVaoCua = now;
                xuat.ThoiGianGioiHan = now.AddMinutes(phutToiDa);
                xuat.TrangThai = (int)TrangThaiXuat.DangBanGiao;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await PushXuatUpdate();
                return Ok(new { success = true, thoiGianGioiHan = xuat.ThoiGianGioiHan, phutToiDa });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi xác nhận!", inner = ex.Message });
            }
        }

        // POST: /NhanVienXuat/BanGiao
        [HttpPost]
        public async Task<IActionResult> BanGiao([FromBody] BanGiaoXuatRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai != (int)TrangThaiXuat.DangBanGiao
                 && xuat.TrangThai != (int)TrangThaiXuat.QuaThoiGian)
                    return BadRequest(new { message = "Phiếu không ở trạng thái bàn giao!" });

                var userId = GetCurrentUserId();
                var now = DateTime.Now;

                foreach (var item in req.Items)
                {
                    var ct = await _context.ChitietXuats
                        .AsTracking()
                        .FirstOrDefaultAsync(c => c.Id == item.ChitietXuatId);
                    if (ct == null) continue;
                    if (item.SoLuong <= 0 || item.SoLuong > ct.ChuaBG) continue;

                    ct.ChuaBG -= item.SoLuong;
                    ct.DaBG += item.SoLuong;

                    // ChiTietDonViId — đúng tên field trong LichSuBanGiao model
                    _context.LichSuBanGiaos.Add(new LichSuBanGiao
                    {
                        LoaiPhieu = "XUAT",
                        PhieuId = req.XuatId,
                        ChiTietDonViId = item.ChitietXuatId,
                        DonVi = item.DonVi,
                        SoBG = item.SoLuong,
                        NhanVienId = userId,
                        ThoiGian = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await PushXuatUpdate();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Lỗi bàn giao!",
                    inner = ex.Message,
                    inner2 = ex.InnerException?.Message,      // ← thêm dòng này
                    inner3 = ex.InnerException?.InnerException?.Message  // ← và dòng này
                });
            }
        }

        // POST: /NhanVienXuat/HoanThanh
        [HttpPost]
        public async Task<IActionResult> HoanThanh([FromBody] XuatIdRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats
                    .AsTracking()
                    .Include(x => x.ChitietXuats)
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });

                var now = DateTime.Now;
                xuat.TrangThai = (int)TrangThaiXuat.HoanThanh;
                xuat.ThoiGianHoanThanh = now;

                if (xuat.ChitietXuats != null)
                    foreach (var ct in xuat.ChitietXuats)
                        ct.ChuaBG = 0; // DaBG giữ nguyên

                // DanhSachXe KHÔNG đổi ở bước này — chờ bảo vệ xác nhận (bước 6)

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await PushXuatUpdate();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hoàn thành!", inner = ex.Message });
            }
        }

        // POST: /NhanVienXuat/QuaThoiGian
        [HttpPost]
        public async Task<IActionResult> QuaThoiGian([FromBody] XuatIdRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai != (int)TrangThaiXuat.DangBanGiao)
                    return Ok(new { success = true, skipped = true });

                xuat.TrangThai = (int)TrangThaiXuat.QuaThoiGian;

                _context.CanhBaos.Add(new CanhBao
                {
                    LoaiPhieu = "XUAT",
                    PhieuId = req.XuatId,
                    LoaiCanhBao = "QuaThoiGian",
                    ThoiGian = DateTime.Now,
                    GhiChu = $"Xe quá thời gian xuất tại cửa {xuat.CuaXuatId}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await PushXuatUpdate();
                await _hubContext.Clients.All.SendAsync("CanhBaoDo", new
                {
                    LoaiPhieu = "XUAT",
                    PhieuId = req.XuatId,
                    CuaXuatId = xuat.CuaXuatId,
                    LoaiCanhBao = "QuaThoiGian",
                    ThoiGian = DateTime.Now
                });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi cập nhật quá thời gian!", inner = ex.Message });
            }
        }

        // ── Helper: push SignalR ──
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

    // ── DTOs ──
    public class XuatIdRequest
    {
        public int XuatId { get; set; }
    }

    public class BanGiaoXuatRequest
    {
        public int XuatId { get; set; }
        public List<BanGiaoXuatItem> Items { get; set; } = new();
    }

    public class BanGiaoXuatItem
    {
        public int ChitietXuatId { get; set; }
        public string DonVi { get; set; } = string.Empty;
        public long SoLuong { get; set; }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Data;
using LedApp.Models;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LedApp.Controllers
{
    [Authorize]
    public class BaoVeXuatController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;

        public BaoVeXuatController(ApplicationDBContext context, IHubContext<SignalServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: /BaoVeXuat
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "";
            ViewBag.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            return View();
        }

        // GET: /BaoVeXuat/GetXeChoXacNhan
        // Danh sách xe đã HoanThanh(3) — chờ bảo vệ xác nhận ra cổng
        [HttpGet]
        public async Task<IActionResult> GetXeChoXacNhan()
        {
            var today = DateTime.Today;
            var xes = await _context.Xuats
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.CuaXuat)
                .Include(x => x.NhanVienXacNhan)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai == (int)TrangThaiXuat.HoanThanh)
                .OrderBy(x => x.ThoiGianHoanThanh)
                .Select(x => new
                {
                    x.Id,
                    x.CuaXuatId,
                    TenCua = x.CuaXuat != null ? x.CuaXuat.Ten : "--",
                    x.XeId,
                    BienSoXe = x.Xe != null ? x.Xe.BienSoXe : "--",
                    LoaiXe = x.Xe != null ? x.Xe.LoaiXe : "--",
                    TaiTrong = x.Xe != null ? x.Xe.TaiTrong : 0,
                    TenTaiXe = x.Xe != null && x.Xe.TaiXe != null ? x.Xe.TaiXe.FullName : "--",
                    TelTaiXe = x.Xe != null && x.Xe.TaiXe != null ? x.Xe.TaiXe.SoDienThoai : "--",
                    TenNhanVien = x.NhanVienXacNhan != null ? x.NhanVienXacNhan.FullName : "--",
                    x.TrangThai,
                    x.ThoiGianPhanCong,
                    x.ThoiGianVaoCua,
                    x.ThoiGianHoanThanh,
                    HangHoas = x.ChitietXuats != null
                        ? x.ChitietXuats.Select(c => new { c.DonVi, c.ChuaBG, c.DaBG }).ToList<object>()
                        : new List<object>()
                })
                .ToListAsync();

            return Json(xes);
        }

        // GET: /BaoVeXuat/GetXeDangVanChuyen
        // Danh sách xe DangVanChuyen — chờ về bãi
        [HttpGet]
        public async Task<IActionResult> GetXeDangVanChuyen()
        {
            // Trong GetXeDangVanChuyen, thêm join với Xuats để lấy thời gian
            var xes = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Where(x => x.TrangThai == (int)TrangThaiXe.DangVanChuyen)
                .OrderBy(x => x.BienSoXe)
                .Select(x => new
                {
                    x.Id,
                    x.BienSoXe,
                    x.LoaiXe,
                    x.TaiTrong,
                    TenTaiXe = x.TaiXe != null ? x.TaiXe.FullName : "--",
                    TelTaiXe = x.TaiXe != null ? x.TaiXe.SoDienThoai : "--",
                    // ── THÊM: lấy từ phiếu xuất gần nhất ──
                    ThoiGianXuatPhat = _context.Xuats
                        .Where(xuat => xuat.XeId == x.Id && xuat.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                        .OrderByDescending(xuat => xuat.ThoiGianXuatPhat)
                        .Select(xuat => xuat.ThoiGianXuatPhat)
                        .FirstOrDefault(),
                    ThoiGianDuKienVeBai = _context.Xuats
                        .Where(xuat => xuat.XeId == x.Id && xuat.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                        .OrderByDescending(xuat => xuat.ThoiGianXuatPhat)
                        .Select(xuat => xuat.ThoiGianDuKienVeBai)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(xes);
        }

        // POST: /BaoVeXuat/XacNhanRaCong
        [HttpPost]
        public async Task<IActionResult> XacNhanRaCong([FromBody] XacNhanRaCongRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai != (int)TrangThaiXuat.HoanThanh)
                    return BadRequest(new { message = "Phiếu chưa hoàn thành bàn giao!" });

                var now = DateTime.Now;
                xuat.TrangThai = (int)TrangThaiXuat.DaXuatPhat;
                xuat.ThoiGianXuatPhat = now;

                // Lưu thời gian dự kiến về bãi vào phiếu xuất
                if (req.ThoiGianDuKienVeBai.HasValue)
                    xuat.ThoiGianDuKienVeBai = req.ThoiGianDuKienVeBai.Value;

                if (xuat.XeId.HasValue)
                {
                    var xe = await _context.DanhSachXes
                        .AsTracking()
                        .FirstOrDefaultAsync(x => x.Id == xuat.XeId.Value);
                    if (xe != null)
                    {
                        xe.TrangThai = (int)TrangThaiXe.DangVanChuyen;
                        // XÓA: xe.ThoiGianDuKienVe không còn dùng nữa
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await PushXuatUpdate();

                return Ok(new { success = true, thoiGianXuatPhat = now });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi xác nhận ra cổng!", inner = ex.Message });
            }
        }

        // POST: /BaoVeXuat/XacNhanVeBai
        [HttpPost]
        public async Task<IActionResult> XacNhanVeBai([FromBody] XacNhanVeBaiRequest req)
        {
            try
            {
                var xe = await _context.DanhSachXes
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XeId);

                if (xe == null)
                    return NotFound(new { message = "Xe không tồn tại!" });
                if (xe.TrangThai != (int)TrangThaiXe.DangVanChuyen)
                    return BadRequest(new { message = "Xe không ở trạng thái đang vận chuyển!" });

                xe.TrangThai = (int)TrangThaiXe.TrongBai;

                // ── THÊM: Tìm phiếu xuất tương ứng và lưu ThoiGianVeBai ──
                var now = DateTime.Now;
                var xuat = await _context.Xuats
                    .AsTracking()
                    .Where(x => x.XeId == xe.Id && x.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                    .OrderByDescending(x => x.ThoiGianXuatPhat)
                    .FirstOrDefaultAsync();

                if (xuat != null)
                    xuat.ThoiGianVeBai = now;

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("XeVeBai", new
                {
                    XeId = xe.Id,
                    BienSoXe = xe.BienSoXe
                });

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi xác nhận về bãi!", inner = ex.Message });
            }
        }
        // POST: /BaoVeXuat/NhanDienBienSo
        // Python camera gọi vào đây để gửi kết quả nhận diện xe xuất
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> NhanDienBienSo([FromBody] NhanDienXuatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BienSo))
                return BadRequest(new { message = "Biển số trống!" });

            var bienSoChuanHoa = req.BienSo.ToUpper()
                .Replace(".", "").Replace("-", "").Trim();

            var now = DateTime.Now;

            // Tìm xe có phiếu xuất HoanThanh hôm nay khớp biển số
            var today = DateTime.Today;
            var xuat = await _context.Xuats
                .AsNoTracking()
                .Include(x => x.Xe)
                .Include(x => x.CuaXuat)
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai == (int)TrangThaiXuat.HoanThanh
                         && x.Xe != null)
                .FirstOrDefaultAsync(x =>
                    x.Xe!.BienSoXe.ToUpper().Replace(".", "").Replace("-", "") == bienSoChuanHoa);

            KetQuaNhanDienXuat ketQua;

            if (xuat != null)
            {
                ketQua = new KetQuaNhanDienXuat
                {
                    BienSo = req.BienSo,
                    TimThay = true,
                    XuatId = xuat.Id,
                    TenCua = xuat.CuaXuat?.Ten ?? "--",
                    TrangThai = xuat.TrangThai,
                    TrangThaiText = "Chờ ra cổng",
                    ThoiGian = now,
                    Confidence = req.Confidence,
                    ThongBao = $"✔ Xe {req.BienSo} — {xuat.CuaXuat?.Ten ?? "--"} — Chờ ra cổng"
                };
            }
            else
            {
                ketQua = new KetQuaNhanDienXuat
                {
                    BienSo = req.BienSo,
                    TimThay = false,
                    XuatId = null,
                    TenCua = "--",
                    TrangThai = -1,
                    TrangThaiText = "--",
                    ThoiGian = now,
                    Confidence = req.Confidence,
                    ThongBao = $"✘ Xe {req.BienSo} không có trong danh sách chờ xuất!"
                };
            }

            // Push SignalR — dùng event riêng "CameraXuatNhanDien" tránh nhầm với nhập
            await _hubContext.Clients.All.SendAsync("CameraXuatNhanDien", ketQua);

            return Ok(new { message = ketQua.ThongBao, data = ketQua });
        }

        // GET: /BaoVeXuat/Status  — Python ping để kiểm tra server online
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Status() => Ok(new
        {
            status = "online",
            time = DateTime.Now,
            message = "Cổng xuất sẵn sàng"
        });
        // ── Helper: build và push tổng hợp xuất ──
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
    }

    // ── DTOs ──
    public class XacNhanRaCongRequest
    {
        public int XuatId { get; set; }
        public DateTime? ThoiGianDuKienVeBai { get; set; }
    }

    public class XacNhanVeBaiRequest
    {
        public int XeId { get; set; }
    }
    // ============================================================
    // THÊM VÀO CUỐI FILE — Request / Response DTOs cho xuất
    // ============================================================

    public class NhanDienXuatRequest
    {
        public string BienSo { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string ThoiGian { get; set; } = string.Empty;
        public string Nguon { get; set; } = "webcam_xuat";
    }

    public class KetQuaNhanDienXuat
    {
        public string BienSo { get; set; } = string.Empty;
        public bool TimThay { get; set; }
        public int? XuatId { get; set; }
        public string TenCua { get; set; } = string.Empty;
        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; } = string.Empty;
        public DateTime ThoiGian { get; set; }
        public double Confidence { get; set; }
        public string ThongBao { get; set; } = string.Empty;
    }
}
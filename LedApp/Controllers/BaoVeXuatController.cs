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
                    TenTaiXe = x.Xe != null && x.Xe.TaiXe != null ? x.Xe.TaiXe.Name : "--",
                    TelTaiXe = x.Xe != null && x.Xe.TaiXe != null ? x.Xe.TaiXe.Tels : "--",
                    TenNhanVien = x.NhanVienXacNhan != null ? x.NhanVienXacNhan.Name : "--",
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
            var xes = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Where(x => x.TrangThai == (int)TrangThaiXe.DangVanChuyen)
                .OrderBy(x => x.ThoiGianDuKienVe)
                .Select(x => new
                {
                    x.Id,
                    x.BienSoXe,
                    x.LoaiXe,
                    x.TaiTrong,
                    x.ThoiGianDuKienVe,
                    TenTaiXe = x.TaiXe != null ? x.TaiXe.Name : "--",
                    TelTaiXe = x.TaiXe != null ? x.TaiXe.Tels : "--"
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

                if (xuat.XeId.HasValue)
                {
                    var xe = await _context.DanhSachXes
                        .AsTracking()
                        .FirstOrDefaultAsync(x => x.Id == xuat.XeId.Value);
                    if (xe != null)
                    {
                        xe.TrangThai = (int)TrangThaiXe.DangVanChuyen;
                        if (req.ThoiGianDuKienVe.HasValue)
                            xe.ThoiGianDuKienVe = req.ThoiGianDuKienVe.Value;
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
                xe.ThoiGianDuKienVe = null; // reset

                await _context.SaveChangesAsync();

                // Push SignalR thông báo xe về bãi
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
        public DateTime? ThoiGianDuKienVe { get; set; }
    }

    public class XacNhanVeBaiRequest
    {
        public int XeId { get; set; }
    }
}
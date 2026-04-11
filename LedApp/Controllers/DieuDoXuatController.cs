using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Data;
using LedApp.Models;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace LedApp.Controllers
{
    [Authorize]
    public class DieuDoXuatController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly SignalServer _signalServer;

        public DieuDoXuatController(ApplicationDBContext context, SignalServer signalServer)
        {
            _context = context;
            _signalServer = signalServer;
        }

        // GET: /DieuDoXuat
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // Chỉ lấy cửa chưa có phiếu đang hoạt động hôm nay
            var cuaXuats = await _context.CuaXuats.ToListAsync();
            var cuaDaBanId = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                .Select(x => x.CuaXuatId)
                .ToListAsync();
            var cuaTrong = cuaXuats.Where(c => !cuaDaBanId.Contains(c.Id)).ToList();

            // Chỉ lấy xe TrongBai
            var xeTrongBai = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai)
                .ToListAsync();

            var nhanViens = await _context.nguoiDungs
                .Where(u => u.Quyen != (int)Quyen.TaiXe)
                .ToListAsync();

            ViewBag.CuaXuats = cuaTrong;   // ← chỉ cửa trống
            ViewBag.CuaXuatAll = cuaXuats;   // ← tất cả cửa (cho sidebar)
            ViewBag.XeTrongBai = xeTrongBai;
            ViewBag.NhanViens = nhanViens;
            return View();
        }

        // GET: /DieuDoXuat/GetDanhSachXe
        [HttpGet]
        public async Task<IActionResult> GetDanhSachXe()
        {
            var xes = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Select(x => new
                {
                    x.Id,
                    x.BienSoXe,
                    x.LoaiXe,
                    x.TaiTrong,
                    x.TrangThai,
                    x.GhiChu,
                    x.ThoiGianDuKienVe,
                    TenTaiXe = x.TaiXe != null ? x.TaiXe.Name : "--",
                    TelTaiXe = x.TaiXe != null ? x.TaiXe.Tels : "--"
                })
                .ToListAsync();
            return Json(xes);
        }

        // GET: /DieuDoXuat/GetPhieuHomNay
        [HttpGet]
        public async Task<IActionResult> GetPhieuHomNay()
        {
            var today = DateTime.Today;
            var phieus = await _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.NhanVienXacNhan)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == today)
                .OrderByDescending(x => x.ThoiGianPhanCong)
                .Select(x => new
                {
                    x.Id,
                    x.CuaXuatId,
                    TenCua = x.CuaXuat != null ? x.CuaXuat.Ten : "--",
                    x.XeId,
                    BienSoXe = x.Xe != null ? x.Xe.BienSoXe : "--",
                    TenTaiXe = x.Xe != null && x.Xe.TaiXe != null ? x.Xe.TaiXe.Name : "--",
                    TenNhanVien = x.NhanVienXacNhan != null ? x.NhanVienXacNhan.Name : "--",
                    x.TrangThai,
                    x.ThoiGianPhanCong,
                    x.ThoiGianVaoCua,
                    x.ThoiGianGioiHan,
                    x.ThoiGianHoanThanh,
                    x.ThoiGianXuatPhat,
                    x.GhiChu,
                    HangHoas = x.ChitietXuats != null
                        ? x.ChitietXuats.Select(c => new { c.Id, c.DonVi, c.ChuaBG, c.DaBG }).ToList<object>()
                        : new List<object>()
                })
                .ToListAsync();
            return Json(phieus);
        }

        // GET: /DieuDoXuat/GetCuaXuatStatus
        [HttpGet]
        public async Task<IActionResult> GetCuaXuatStatus()
        {
            var today = DateTime.Today;
            var cuaXuats = await _context.CuaXuats.ToListAsync();
            var phieuActive = await _context.Xuats
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                .ToListAsync();

            var result = cuaXuats.Select(c =>
            {
                var phieu = phieuActive.FirstOrDefault(p => p.CuaXuatId == c.Id);
                return new
                {
                    c.Id,
                    c.Ten,
                    CoXe = phieu != null,
                    XuatId = phieu?.Id,
                    BienSoXe = phieu?.Xe?.BienSoXe ?? "--",
                    TenTaiXe = phieu?.Xe?.TaiXe?.Name ?? "--",
                    TrangThaiXuat = phieu?.TrangThai ?? -1,
                    ThoiGianVaoCua = phieu?.ThoiGianVaoCua,
                    ThoiGianGioiHan = phieu?.ThoiGianGioiHan,
                    HangHoas = phieu?.ChitietXuats?.Select(ct => new
                    {
                        ct.DonVi,
                        ct.ChuaBG,
                        ct.DaBG
                    }).ToList()
                };
            }).ToList();

            return Json(result);
        }

        // POST: /DieuDoXuat/TaoPhanCong
        [HttpPost]
        public async Task<IActionResult> TaoPhanCong([FromBody] TaoPhanCongRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra xe
                var xe = await _context.DanhSachXes
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.XeId);
                if (xe == null)
                    return BadRequest(new { message = "Xe không tồn tại!" });
                if (xe.TrangThai != (int)TrangThaiXe.TrongBai)
                    return BadRequest(new { message = "Xe không ở trạng thái Trong bãi!" });

                // Kiểm tra cửa chưa bị dùng hôm nay
                var today = DateTime.Today;
                var cuaDangDung = await _context.Xuats.AnyAsync(x =>
                    x.CuaXuatId == req.CuaXuatId
                    && x.ThoiGianPhanCong.Date == today
                    && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat);
                if (cuaDangDung)
                    return BadRequest(new { message = "Cửa xuất này đang có xe, chọn cửa khác!" });

                // Tạo phiếu xuất
                var xuat = new Xuat
                {
                    CuaXuatId = req.CuaXuatId,
                    XeId = req.XeId,
                    ThoiGianPhanCong = req.ThoiGianPhanCong ?? DateTime.Now,
                    NhanVienXacNhanId = req.NhanVienXacNhanId,
                    GhiChu = req.GhiChu,
                    TrangThai = (int)TrangThaiXuat.DaPhanCong
                };
                _context.Xuats.Add(xuat);
                await _context.SaveChangesAsync(); // lấy xuat.Id

                // INSERT ChitietXuat
                if (req.HangHoas != null)
                {
                    foreach (var h in req.HangHoas)
                    {
                        if (string.IsNullOrEmpty(h.DonVi) || h.SoLuong <= 0) continue;
                        _context.ChitietXuats.Add(new ChitietXuat
                        {
                            XuatId = xuat.Id,
                            DonVi = h.DonVi,
                            ChuaBG = h.SoLuong,
                            DaBG = 0
                        });
                    }
                }

                // FIX: Đổi trạng thái xe CÙNG SaveChangesAsync với ChitietXuat
                xe.TrangThai = (int)TrangThaiXe.DangPhanCong;
                await _context.SaveChangesAsync(); // save cả chitiet + xe cùng lúc

                await transaction.CommitAsync();
                await _signalServer.SendTongHopXuatFull();

                return Ok(new { success = true, xuatId = xuat.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi tạo phiếu!", inner = ex.Message });
            }
        }

        // POST: /DieuDoXuat/HuyPhanCong
        [HttpPost]
        public async Task<IActionResult> HuyPhanCong([FromBody] XuatHuyRequest req)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // FIX: Dùng AsTracking + tách riêng query xe để tránh EF tracking conflict
                var xuat = await _context.Xuats
                    .AsTracking()
                    .Include(x => x.ChitietXuats)
                    .FirstOrDefaultAsync(x => x.Id == req.XuatId);

                if (xuat == null)
                    return NotFound(new { message = "Phiếu không tồn tại!" });
                if (xuat.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                    return BadRequest(new { message = "Xe đã xuất phát, không thể hủy!" });

                // Lưu XeId trước khi xóa
                var xeId = xuat.XeId;

                // Xóa ChitietXuat trước
                if (xuat.ChitietXuats != null && xuat.ChitietXuats.Any())
                    _context.ChitietXuats.RemoveRange(xuat.ChitietXuats);

                // Xóa phiếu xuất
                _context.Xuats.Remove(xuat);
                await _context.SaveChangesAsync();

                // FIX: Query xe SAU KHI đã remove xuat để tránh tracking conflict
                if (xeId.HasValue)
                {
                    var xe = await _context.DanhSachXes
                        .AsTracking()
                        .FirstOrDefaultAsync(x => x.Id == xeId.Value);
                    if (xe != null)
                    {
                        xe.TrangThai = (int)TrangThaiXe.TrongBai;
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                await _signalServer.SendTongHopXuatFull();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hủy phiếu!", inner = ex.Message });
            }
        }

        // ===== THÊM ENDPOINT MỚI VÀO DieuDoXuatController.cs =====

        // GET: /DieuDoXuat/GetXeTrongBai — xe rảnh hiện tại
        [HttpGet]
        public async Task<IActionResult> GetXeTrongBai()
        {
            var xes = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai)
                .Select(x => new
                {
                    x.Id,
                    x.BienSoXe,
                    x.LoaiXe,
                    x.TaiTrong,
                    TenTaiXe = x.TaiXe != null ? x.TaiXe.Name : "Chưa có tài xế",
                    TelTaiXe = x.TaiXe != null ? x.TaiXe.Tels : "--"
                })
                .ToListAsync();
            return Json(xes);
        }

        // GET: /DieuDoXuat/GetCuaTrong — cửa chưa có xe hôm nay
        [HttpGet]
        public async Task<IActionResult> GetCuaTrong()
        {
            var today = DateTime.Today;
            var cuaDangDungIds = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                .Select(x => x.CuaXuatId)
                .ToListAsync();

            var cuaTrong = await _context.CuaXuats
                .Where(c => !cuaDangDungIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Ten })
                .ToListAsync();

            return Json(cuaTrong);
        }
    }

    // ── DTOs ──
    public class TaoPhanCongRequest
    {
        public int XeId { get; set; }
        public int CuaXuatId { get; set; }
        public DateTime? ThoiGianPhanCong { get; set; }
        public int? NhanVienXacNhanId { get; set; }
        public string? GhiChu { get; set; }
        public List<HangHoaInput>? HangHoas { get; set; }
    }

    public class HangHoaInput
    {
        public string DonVi { get; set; } = string.Empty;
        public long SoLuong { get; set; }
    }

    public class XuatHuyRequest
    {
        public int XuatId { get; set; }
    }
}
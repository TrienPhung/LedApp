using LedApp.Data;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDBContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // ── 1. NHẬP hôm nay ──
                var nhaps = await _context.Nhaps
                    .AsNoTracking()
                    .Where(n => n.ThoiGianPhanCong >= today && n.ThoiGianPhanCong < tomorrow)
                    .ToListAsync();

                var nhapStats = new
                {
                    TongSo = nhaps.Count,
                    HoanThanh = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                    DangNhap = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao),
                    QuaGio = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                    PhanCong = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong),
                };

                // ── 2. XUẤT hôm nay ──
                var xuats = await _context.Xuats
                    .AsNoTracking()
                    .Where(x => x.ThoiGianPhanCong >= today && x.ThoiGianPhanCong < tomorrow)
                    .ToListAsync();

                var xuatStats = new
                {
                    TongSo = xuats.Count,
                    HoanThanh = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh
                                              || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                    DangXuat = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                    QuaGio = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                    PhanCong = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaPhanCong),
                };

                // ── 3. Tích lũy tất cả thời gian ──
                var tongNhapTatCa = await _context.Nhaps.AsNoTracking()
                    .CountAsync(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh);
                var tongXuatTatCa = await _context.Xuats.AsNoTracking()
                    .CountAsync(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh
                                  || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat);

                // ── 4. Cảnh báo ──
                var canhBaoChuaXuLy = await _context.CanhBaos.AsNoTracking()
                    .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                var canhBaoQuaGio = await _context.CanhBaos.AsNoTracking()
                    .CountAsync(c => c.LoaiCanhBao == "QuaHanNhap"
                                  && c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);

                // ── 5. Cửa nhập đang hoạt động ──
                var cuaNhapStatus = await _context.CuaNhaps
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.Id,
                        c.Ten,
                        NhapHienTai = c.Nhaps
                            .Where(n => n.TrangThai != (int)TrangThaiNhap.HoanThanh)
                            .OrderByDescending(n => n.ThoiGianPhanCong)
                            .Select(n => new { n.BienSoXe, n.TrangThai, n.ThoiGianGioiHan })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                // ── 6. Biểu đồ theo giờ hôm nay ──
                var lichSu = await _context.LichSuBanGiaos
                    .AsNoTracking()
                    .Where(l => l.ThoiGian >= today && l.ThoiGian < tomorrow)
                    .GroupBy(l => new { l.ThoiGian.Hour, l.LoaiPhieu })
                    .Select(g => new { Gio = g.Key.Hour, Loai = g.Key.LoaiPhieu, TongBG = g.Sum(x => x.SoBG) })
                    .ToListAsync();

                var chartNhap = new long[24];
                var chartXuat = new long[24];
                foreach (var item in lichSu)
                {
                    if (item.Loai == "NHAP") chartNhap[item.Gio] += item.TongBG;
                    else if (item.Loai == "XUAT") chartXuat[item.Gio] += item.TongBG;
                }

                // ── 7. KPI thời gian bàn giao hôm nay ──
                var kpiNhap = nhaps
                    .Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                    .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();

                var kpiXuat = xuats
                    .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                    .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();

                return Ok(new
                {
                    nhap = nhapStats,
                    xuat = xuatStats,
                    tichLuy = new { tongNhap = tongNhapTatCa, tongXuat = tongXuatTatCa },
                    canhBao = new { TongChuaXuLy = canhBaoChuaXuLy, QuaGio = canhBaoQuaGio },
                    cuaNhap = cuaNhapStatus,
                    chart = new { nhap = chartNhap, xuat = chartXuat },
                    kpi = new
                    {
                        nhapTb = kpiNhap.Any() ? Math.Round(kpiNhap.Average(), 1) : 0,
                        xuatTb = kpiXuat.Any() ? Math.Round(kpiXuat.Average(), 1) : 0,
                    },
                    capNhat = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetStats dashboard");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
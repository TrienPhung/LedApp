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
        [Authorize(Policy = "Dashboard.View")]
        public IActionResult Index() => View();

        [HttpGet]
        [Authorize(Policy = "Dashboard.View")]
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
                    tongSo = nhaps.Count,
                    hoanThanh = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                    dangNhap = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao),
                    quaGio = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                    phanCong = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong),
                };

                // ── 2. XUẤT hôm nay ──
                var xuats = await _context.Xuats
                    .AsNoTracking()
                    .Where(x => x.ThoiGianPhanCong >= today && x.ThoiGianPhanCong < tomorrow)
                    .ToListAsync();

                var xuatStats = new
                {
                    tongSo = xuats.Count,
                    hoanThanh = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh
                                              || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                    dangXuat = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                    quaGio = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                    phanCong = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaPhanCong),
                };

                // ── 3. Tích lũy ──
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

                // ── 5. Cửa nhập — FIX: load rõ ràng tên cửa ──
                var cuaNhapRaw = await _context.CuaNhaps
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Include(c => c.Nhaps)
                    .ToListAsync();

                var cuaNhapStatus = cuaNhapRaw.Select(c =>
                {
                    var nhapHienTai = c.Nhaps?
                        .Where(n => n.TrangThai != (int)TrangThaiNhap.HoanThanh)
                        .OrderByDescending(n => n.ThoiGianPhanCong)
                        .FirstOrDefault();

                    return new
                    {
                        id = c.Id,
                        ten = c.Ten ?? $"Cửa {c.Id}",          // ← FIX: đảm bảo không null
                        mota = c.Mota,
                        nhapHienTai = nhapHienTai == null ? null : new
                        {
                            bienSoXe = nhapHienTai.BienSoXe,
                            trangThai = nhapHienTai.TrangThai,
                            thoiGianGioiHan = nhapHienTai.ThoiGianGioiHan
                        }
                    };
                }).ToList();

                // ── 6. Cửa xuất ──
                var cuaXuatRaw = await _context.CuaXuats
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .Include(c => c.Xuats)
                    .ToListAsync();

                var cuaXuatStatus = cuaXuatRaw.Select(c =>
                {
                    var xuatHienTai = c.Xuats?
                        .Where(x => x.TrangThai != (int)TrangThaiXuat.HoanThanh
                                 && x.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                        .OrderByDescending(x => x.ThoiGianPhanCong)
                        .FirstOrDefault();

                    return new
                    {
                        id = c.Id,
                        ten = c.Ten ?? $"Cửa xuất {c.Id}",
                        mota = c.Mota,
                        xuatHienTai = xuatHienTai == null ? null : new
                        {
                            trangThai = xuatHienTai.TrangThai,
                            thoiGianGioiHan = xuatHienTai.ThoiGianGioiHan
                        }
                    };
                }).ToList();

                // ── 7. Biểu đồ theo giờ ──
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

                // ── 8. KPI ──
                var kpiNhap = nhaps
                    .Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                    .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();

                var kpiXuat = xuats
                    .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                    .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();

                // ── 9. [MỚI] Xe đang chờ realtime ──
                var xeDangCho = await _context.Nhaps
                    .AsNoTracking()
                    .Where(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong
                             || n.TrangThai == (int)TrangThaiNhap.DangBanGiao)
                    .OrderBy(n => n.ThoiGianGioiHan)
                    .Take(10)
                    .Select(n => new
                    {
                        bienSoXe = n.BienSoXe,
                        trangThai = n.TrangThai,
                        thoiGianGioiHan = n.ThoiGianGioiHan,
                        cuaNhapId = n.CuaNhapId,
                        ghiChu = n.GhiChu
                    })
                    .ToListAsync();

                // Join tên cửa vào danh sách xe chờ
                var xeDangChoFull = xeDangCho.Select(x => new
                {
                    x.bienSoXe,
                    x.trangThai,
                    x.thoiGianGioiHan,
                    x.ghiChu,
                    tenCua = cuaNhapRaw.FirstOrDefault(c => c.Id == x.cuaNhapId)?.Ten ?? $"Cửa {x.cuaNhapId}"
                }).ToList();

                // ── 10. [MỚI] Thống kê nhân viên hôm nay ──
                var nhanVienStats = await _context.LichSuBanGiaos
                    .AsNoTracking()
                    .Where(l => l.ThoiGian >= today && l.ThoiGian < tomorrow && l.NhanVienId.HasValue)
                    .GroupBy(l => l.NhanVienId)
                    .Select(g => new
                    {
                        nhanVienId = g.Key,
                        tongBG = g.Sum(x => x.SoBG),
                        soLuot = g.Count()
                    })
                    .OrderByDescending(x => x.tongBG)
                    .Take(5)
                    .ToListAsync();

                // Join tên nhân viên
                var nhanVienIds = nhanVienStats.Select(x => x.nhanVienId).ToList();
                var nhanViens = await _context.nguoiDungs
                    .AsNoTracking()
                    .Where(nv => nhanVienIds.Contains(nv.Id))
                    .Select(nv => new { nv.Id, tenNV = nv.LastName + " " + nv.FirstName })
                    .ToListAsync();

                var nhanVienStatsFull = nhanVienStats.Select(x => new
                {
                    x.tongBG,
                    x.soLuot,
                    tenNhanVien = nhanViens.FirstOrDefault(nv => nv.Id == x.nhanVienId)?.tenNV ?? "N/A"
                }).ToList();

                return Ok(new
                {
                    nhap = nhapStats,
                    xuat = xuatStats,
                    tichLuy = new { tongNhap = tongNhapTatCa, tongXuat = tongXuatTatCa },
                    canhBao = new { tongChuaXuLy = canhBaoChuaXuLy, quaGio = canhBaoQuaGio },
                    cuaNhap = cuaNhapStatus,
                    cuaXuat = cuaXuatStatus,
                    chart = new { nhap = chartNhap, xuat = chartXuat },
                    kpi = new
                    {
                        nhapTb = kpiNhap.Any() ? Math.Round(kpiNhap.Average(), 1) : 0,
                        xuatTb = kpiXuat.Any() ? Math.Round(kpiXuat.Average(), 1) : 0,
                    },
                    xeDangCho = xeDangChoFull,
                    nhanVienStats = nhanVienStatsFull,
                    capNhat = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetStats dashboard");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ── [MỚI] Export Excel báo cáo hôm nay ──
        [HttpGet]
        [Authorize(Policy = "Dashboard.Export")]
        public async Task<IActionResult> ExportExcel()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var nhaps = await _context.Nhaps
                    .AsNoTracking()
                    .Include(n => n.CuaNhap)
                    .Include(n => n.NhanVienXacNhan)
                    .Where(n => n.ThoiGianPhanCong >= today && n.ThoiGianPhanCong < tomorrow)
                    .ToListAsync();

                var xuats = await _context.Xuats
                    .AsNoTracking()
                    .Include(x => x.CuaXuat)
                    .Include(x => x.NhanVienXacNhan)
                    .Where(x => x.ThoiGianPhanCong >= today && x.ThoiGianPhanCong < tomorrow)
                    .ToListAsync();

                // Build CSV nhanh (không cần thư viện ngoài)
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("BÁO CÁO NHẬP XUẤT KHO - " + today.ToString("dd/MM/yyyy"));
                sb.AppendLine();
                sb.AppendLine("=== PHIẾU NHẬP ===");
                sb.AppendLine("STT,Biển số xe,Cửa nhập,Trạng thái,Giờ phân công,Giờ hoàn thành,NV xác nhận");

                var ttNhapMap = new Dictionary<int, string>
                {
                    {0,"Đã phân công"},{1,"Đang bàn giao"},{2,"Quá thời gian"},{3,"Hoàn thành"}
                };
                int stt = 1;
                foreach (var n in nhaps)
                {
                    sb.AppendLine($"{stt++},{n.BienSoXe},{n.CuaNhap?.Ten ?? ""},{ttNhapMap.GetValueOrDefault(n.TrangThai, "?")},{n.ThoiGianPhanCong:HH:mm},{n.ThoiGianHoanThanh?.ToString("HH:mm") ?? ""},{n.NhanVienXacNhan?.LastName + " " + n.NhanVienXacNhan?.FirstName ?? ""}");
                }

                sb.AppendLine();
                sb.AppendLine("=== PHIẾU XUẤT ===");
                sb.AppendLine("STT,Cửa xuất,Trạng thái,Giờ phân công,Giờ hoàn thành,NV xác nhận");

                var ttXuatMap = new Dictionary<int, string>
                {
                    {0,"Đã phân công"},{1,"Đang bàn giao"},{2,"Quá thời gian"},{3,"Hoàn thành"},{4,"Đã xuất phát"}
                };
                stt = 1;
                foreach (var x in xuats)
                {
                    sb.AppendLine($"{stt++},{x.CuaXuat?.Ten ?? ""},{ttXuatMap.GetValueOrDefault(x.TrangThai, "?")},{x.ThoiGianPhanCong:HH:mm},{x.ThoiGianHoanThanh?.ToString("HH:mm") ?? ""},{x.NhanVienXacNhan?.LastName + " " + x.NhanVienXacNhan?.FirstName ?? ""}");
                }

                var bytes = System.Text.Encoding.UTF8.GetPreamble()
                    .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
                    .ToArray();

                return File(bytes, "text/csv", $"BaoCao_{today:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi ExportExcel");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
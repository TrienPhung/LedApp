using ClosedXML.Excel;
using LedApp.Data;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Controllers
{
    [Authorize]
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<BaoCaoController> _logger;

        public BaoCaoController(ApplicationDBContext context, ILogger<BaoCaoController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index() => View();

        // ── GET /BaoCao/GetBaoCao?tuNgay=&denNgay=
        [HttpGet]
        public async Task<IActionResult> GetBaoCao(string? tuNgay, string? denNgay)
        {
            try
            {
                var from = string.IsNullOrEmpty(tuNgay)
                    ? DateTime.Today.AddDays(-30)
                    : DateTime.Parse(tuNgay);
                var to = string.IsNullOrEmpty(denNgay)
                    ? DateTime.Today.AddDays(1)
                    : DateTime.Parse(denNgay).AddDays(1);

                // ── 1. TỔNG NHẬP ──
                var nhaps = await _context.Nhaps
                    .AsNoTracking()
                    .Include(n => n.ChitietNhaps)
                    .Include(n => n.CuaNhap)
                    .Where(n => n.ThoiGianPhanCong >= from && n.ThoiGianPhanCong < to)
                    .ToListAsync();

                var tongNhap = nhaps.Count;
                var nhapHoanThanh = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh);
                var nhapQuaGio = nhaps.Count(n =>
                    n.TrangThai == (int)TrangThaiNhap.QuaThoiGian ||
                    (n.TrangThai == (int)TrangThaiNhap.HoanThanh &&
                     n.ThoiGianHoanThanh.HasValue && n.ThoiGianGioiHan.HasValue &&
                     n.ThoiGianHoanThanh > n.ThoiGianGioiHan));

                // KPI nhập (phút: ThoiGianVaoCua → ThoiGianHoanThanh)
                var nhapCoTG = nhaps
                    .Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                    .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();
                var kpiNhapTb = nhapCoTG.Any() ? Math.Round(nhapCoTG.Average(), 1) : 0;
                var kpiNhapMin = nhapCoTG.Any() ? Math.Round(nhapCoTG.Min(), 1) : 0;
                var kpiNhapMax = nhapCoTG.Any() ? Math.Round(nhapCoTG.Max(), 1) : 0;

                // ── 2. TỔNG XUẤT ──
                var xuats = await _context.Xuats
                    .AsNoTracking()
                    .Include(x => x.ChitietXuats)
                    .Include(x => x.CuaXuat)
                    .Include(x => x.Xe)
                    .Where(x => x.ThoiGianPhanCong >= from && x.ThoiGianPhanCong < to)
                    .ToListAsync();

                var tongXuat = xuats.Count;
                var xuatHoanThanh = xuats.Count(x =>
                    x.TrangThai == (int)TrangThaiXuat.HoanThanh ||
                    x.TrangThai == (int)TrangThaiXuat.DaXuatPhat);
                var xuatQuaGio = xuats.Count(x =>
                    x.TrangThai == (int)TrangThaiXuat.QuaThoiGian ||
                    (x.ThoiGianHoanThanh.HasValue && x.ThoiGianGioiHan.HasValue &&
                     x.ThoiGianHoanThanh > x.ThoiGianGioiHan));

                // KPI xuất
                var xuatCoTG = xuats
                    .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                    .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();
                var kpiXuatTb = xuatCoTG.Any() ? Math.Round(xuatCoTG.Average(), 1) : 0;
                var kpiXuatMin = xuatCoTG.Any() ? Math.Round(xuatCoTG.Min(), 1) : 0;
                var kpiXuatMax = xuatCoTG.Any() ? Math.Round(xuatCoTG.Max(), 1) : 0;

                // ── 3. XE HAY QUÁ HẠN (từ bảng CanhBao) ──
                // CanhBao.GhiChu lưu dạng "Xe:29H-12345|Cua:1|GioiHan:..."
                var canhBaos = await _context.CanhBaos
                    .AsNoTracking()
                    .Where(c => c.LoaiCanhBao == "QuaHanNhap"
                             && c.ThoiGian >= from && c.ThoiGian < to)
                    .ToListAsync();

                var xeQuaHanList = canhBaos
                    .GroupBy(c => c.GhiChu)
                    .Select(g =>
                    {
                        var parts = (g.Key ?? "").Split('|');
                        var bienSo = parts.FirstOrDefault(p => p.StartsWith("Xe:"))?.Replace("Xe:", "") ?? "--";
                        var cua = parts.FirstOrDefault(p => p.StartsWith("Cua:"))?.Replace("Cua:", "") ?? "--";
                        return new
                        {
                            bienSo,
                            cua,
                            soLan = g.Count(),
                            lanCuoi = g.Max(x => x.ThoiGian)
                        };
                    })
                    .OrderByDescending(x => x.soLan)
                    .Take(10)
                    .ToList();

                // ── 4. BIỂU ĐỒ NHẬP/XUẤT THEO NGÀY (từ LichSuBanGiao) ──
                var lichSu = await _context.LichSuBanGiaos
                    .AsNoTracking()
                    .Where(l => l.ThoiGian >= from && l.ThoiGian < to)
                    .GroupBy(l => new { l.ThoiGian.Date, l.LoaiPhieu })
                    .Select(g => new
                    {
                        Ngay = g.Key.Date,
                        Loai = g.Key.LoaiPhieu,
                        SoLuot = g.Count()
                    })
                    .OrderBy(g => g.Ngay)
                    .ToListAsync();

                var chartData = new List<object>();
                for (var d = from.Date; d < to.Date; d = d.AddDays(1))
                {
                    chartData.Add(new
                    {
                        ngay = d.ToString("dd/MM"),
                        nhap = lichSu.Where(l => l.Ngay == d && l.Loai == "NHAP").Sum(l => l.SoLuot),
                        xuat = lichSu.Where(l => l.Ngay == d && l.Loai == "XUAT").Sum(l => l.SoLuot)
                    });
                }

                // ── 5. CHI TIẾT NHẬP theo ngày ──
                var chitietNhap = nhaps
                    .GroupBy(n => n.ThoiGianPhanCong.Date)
                    .Select(g => new
                    {
                        ngay = g.Key.ToString("dd/MM/yyyy"),
                        tongSo = g.Count(),
                        hoanThanh = g.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                        quaGio = g.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                        dangNhap = g.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao)
                    })
                    .OrderByDescending(g => g.ngay)
                    .ToList();

                // ── 6. CHI TIẾT XUẤT theo ngày ──
                var chitietXuat = xuats
                    .GroupBy(x => x.ThoiGianPhanCong.Date)
                    .Select(g => new
                    {
                        ngay = g.Key.ToString("dd/MM/yyyy"),
                        tongSo = g.Count(),
                        hoanThanh = g.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh
                                               || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                        quaGio = g.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                        dangXuat = g.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao)
                    })
                    .OrderByDescending(g => g.ngay)
                    .ToList();

                // ── 7. TOP CỬA NHẬP (cho chart Revenue by Category kiểu Apex) ──
                var topCuaNhap = nhaps
                    .GroupBy(n => n.CuaNhap?.Ten ?? "Không rõ")
                    .Select(g => new
                    {
                        ten = g.Key,
                        soPhieu = g.Count(),
                        hoanThanh = g.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh)
                    })
                    .OrderByDescending(g => g.soPhieu)
                    .Take(5)
                    .ToList();

                // ── 8. TOP CỬA XUẤT ──
                var topCuaXuat = xuats
                    .GroupBy(x => x.CuaXuat?.Ten ?? "Không rõ")
                    .Select(g => new
                    {
                        ten = g.Key,
                        soPhieu = g.Count(),
                        hoanThanh = g.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh
                                              || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                    })
                    .OrderByDescending(g => g.soPhieu)
                    .Take(5)
                    .ToList();

                return Ok(new
                {
                    tuNgay = from.ToString("dd/MM/yyyy"),
                    denNgay = to.AddDays(-1).ToString("dd/MM/yyyy"),
                    tongHop = new
                    {
                        tongNhap,
                        nhapHoanThanh,
                        nhapQuaGio,
                        nhapDangXuLy = nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao),
                        tongXuat,
                        xuatHoanThanh,
                        xuatQuaGio,
                        xuatDangXuLy = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                        kpi = new
                        {
                            nhapTb = kpiNhapTb,
                            nhapMin = kpiNhapMin,
                            nhapMax = kpiNhapMax,
                            xuatTb = kpiXuatTb,
                            xuatMin = kpiXuatMin,
                            xuatMax = kpiXuatMax
                        }
                    },
                    chart = chartData,
                    topCuaNhap,
                    topCuaXuat,
                    chitietNhap,
                    chitietXuat,
                    xeQuaHan = xeQuaHanList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetBaoCao");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ── GET /BaoCao/ExportExcel?tuNgay=&denNgay=
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string? tuNgay, string? denNgay)
        {
            var from = string.IsNullOrEmpty(tuNgay)
                ? DateTime.Today.AddDays(-30)
                : DateTime.Parse(tuNgay);
            var to = string.IsNullOrEmpty(denNgay)
                ? DateTime.Today.AddDays(1)
                : DateTime.Parse(denNgay).AddDays(1);

            var nhaps = await _context.Nhaps
                .AsNoTracking()
                .Include(n => n.CuaNhap)
                .Where(n => n.ThoiGianPhanCong >= from && n.ThoiGianPhanCong < to)
                .OrderByDescending(n => n.ThoiGianPhanCong)
                .ToListAsync();

            var xuats = await _context.Xuats
                .AsNoTracking()
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Where(x => x.ThoiGianPhanCong >= from && x.ThoiGianPhanCong < to)
                .OrderByDescending(x => x.ThoiGianPhanCong)
                .ToListAsync();

            var canhBaos = await _context.CanhBaos
                .AsNoTracking()
                .Where(c => c.LoaiCanhBao == "QuaHanNhap"
                         && c.ThoiGian >= from && c.ThoiGian < to)
                .OrderByDescending(c => c.ThoiGian)
                .ToListAsync();

            using var wb = new XLWorkbook();

            // ── Sheet 1: Tổng hợp ──
            var ws1 = wb.Worksheets.Add("Tổng hợp");
            ws1.Cell("A1").Value = $"BÁO CÁO THỐNG KÊ — Từ {from:dd/MM/yyyy} đến {to.AddDays(-1):dd/MM/yyyy}";
            ws1.Cell("A1").Style.Font.Bold = true;
            ws1.Cell("A1").Style.Font.FontSize = 14;
            ws1.Range("A1:F1").Merge();

            var headers1 = new[] { "Chỉ số", "Nhập", "Xuất" };
            for (int i = 0; i < headers1.Length; i++)
            {
                ws1.Cell(3, i + 1).Value = headers1[i];
                ws1.Cell(3, i + 1).Style.Font.Bold = true;
                ws1.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
                ws1.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
                ws1.Cell(3, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var nhapKpi = nhaps
                .Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes)
                .DefaultIfEmpty(0).ToList();
            var xuatKpi = xuats
                .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                .DefaultIfEmpty(0).ToList();

            var rows1 = new[]
            {
                new[] { "Tổng phiếu",
                    nhaps.Count.ToString(),
                    xuats.Count.ToString() },
                new[] { "Hoàn thành",
                    nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh).ToString(),
                    xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat).ToString() },
                new[] { "Quá giờ",
                    nhaps.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian).ToString(),
                    xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian).ToString() },
                new[] { "KPI Trung bình (phút)",
                    Math.Round(nhapKpi.Average(), 1).ToString(),
                    Math.Round(xuatKpi.Average(), 1).ToString() },
                new[] { "KPI Nhanh nhất (phút)",
                    Math.Round(nhapKpi.Min(), 1).ToString(),
                    Math.Round(xuatKpi.Min(), 1).ToString() },
                new[] { "KPI Chậm nhất (phút)",
                    Math.Round(nhapKpi.Max(), 1).ToString(),
                    Math.Round(xuatKpi.Max(), 1).ToString() },
            };

            for (int r = 0; r < rows1.Length; r++)
                for (int c = 0; c < rows1[r].Length; c++)
                {
                    ws1.Cell(r + 4, c + 1).Value = rows1[r][c];
                    if (c > 0) ws1.Cell(r + 4, c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            ws1.Columns().AdjustToContents();

            // ── Sheet 2: Chi tiết nhập ──
            var ws2 = wb.Worksheets.Add("Chi tiết nhập");
            var hdNhap = new[] { "STT", "Biển số xe", "Cửa nhập", "Phân công", "Vào cửa", "Giới hạn", "Hoàn thành", "TG bàn giao (phút)", "Trạng thái" };
            for (int i = 0; i < hdNhap.Length; i++)
            {
                ws2.Cell(1, i + 1).Value = hdNhap[i];
                ws2.Cell(1, i + 1).Style.Font.Bold = true;
                ws2.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
                ws2.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var ttNhapMap = new Dictionary<int, string>
            {
                { (int)TrangThaiNhap.DaPhanCong,  "Đã phân công" },
                { (int)TrangThaiNhap.DangBanGiao, "Đang bàn giao" },
                { (int)TrangThaiNhap.QuaThoiGian, "Quá giờ" },
                { (int)TrangThaiNhap.HoanThanh,   "Hoàn thành" }
            };

            for (int i = 0; i < nhaps.Count; i++)
            {
                var n = nhaps[i];
                double tgBG = (n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                    ? Math.Round((n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes, 1) : 0;

                ws2.Cell(i + 2, 1).Value = i + 1;
                ws2.Cell(i + 2, 2).Value = n.BienSoXe;
                ws2.Cell(i + 2, 3).Value = n.CuaNhap?.Ten ?? "--";
                ws2.Cell(i + 2, 4).Value = n.ThoiGianPhanCong.ToString("dd/MM/yyyy HH:mm");
                ws2.Cell(i + 2, 5).Value = n.ThoiGianVaoCua?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 6).Value = n.ThoiGianGioiHan?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 7).Value = n.ThoiGianHoanThanh?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 8).Value = tgBG > 0 ? tgBG.ToString() : "--";
                ws2.Cell(i + 2, 9).Value = ttNhapMap.GetValueOrDefault(n.TrangThai, "--");

                if (n.TrangThai == (int)TrangThaiNhap.QuaThoiGian)
                    ws2.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
                else if (n.TrangThai == (int)TrangThaiNhap.HoanThanh)
                    ws2.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fff4");
            }
            ws2.Columns().AdjustToContents();

            // ── Sheet 3: Chi tiết xuất ──
            var ws3 = wb.Worksheets.Add("Chi tiết xuất");
            var hdXuat = new[] { "STT", "Biển số xe", "Cửa xuất", "Phân công", "Vào cửa", "Giới hạn", "Hoàn thành", "TG bàn giao (phút)", "Trạng thái" };
            for (int i = 0; i < hdXuat.Length; i++)
            {
                ws3.Cell(1, i + 1).Value = hdXuat[i];
                ws3.Cell(1, i + 1).Style.Font.Bold = true;
                ws3.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0f6e56");
                ws3.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var ttXuatMap = new Dictionary<int, string>
            {
                { (int)TrangThaiXuat.DaPhanCong,  "Đã phân công" },
                { (int)TrangThaiXuat.DangBanGiao, "Đang bàn giao" },
                { (int)TrangThaiXuat.QuaThoiGian, "Quá giờ" },
                { (int)TrangThaiXuat.HoanThanh,   "Hoàn thành" },
                { (int)TrangThaiXuat.DaXuatPhat,  "Đã xuất phát" }
            };

            for (int i = 0; i < xuats.Count; i++)
            {
                var x = xuats[i];
                double tgBG = (x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                    ? Math.Round((x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes, 1) : 0;

                ws3.Cell(i + 2, 1).Value = i + 1;
                ws3.Cell(i + 2, 2).Value = x.Xe?.BienSoXe ?? "--";
                ws3.Cell(i + 2, 3).Value = x.CuaXuat?.Ten ?? "--";
                ws3.Cell(i + 2, 4).Value = x.ThoiGianPhanCong.ToString("dd/MM/yyyy HH:mm");
                ws3.Cell(i + 2, 5).Value = x.ThoiGianVaoCua?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 6).Value = x.ThoiGianGioiHan?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 7).Value = x.ThoiGianHoanThanh?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 8).Value = tgBG > 0 ? tgBG.ToString() : "--";
                ws3.Cell(i + 2, 9).Value = ttXuatMap.GetValueOrDefault(x.TrangThai, "--");

                if (x.TrangThai == (int)TrangThaiXuat.QuaThoiGian)
                    ws3.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
                else if (x.TrangThai == (int)TrangThaiXuat.HoanThanh || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                    ws3.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fff4");
            }
            ws3.Columns().AdjustToContents();

            // ── Sheet 4: Xe quá hạn ──
            var ws4 = wb.Worksheets.Add("Xe quá hạn");
            var hdCB = new[] { "STT", "Biển số", "Cửa", "Thời gian cảnh báo", "Ghi chú" };
            for (int i = 0; i < hdCB.Length; i++)
            {
                ws4.Cell(1, i + 1).Value = hdCB[i];
                ws4.Cell(1, i + 1).Style.Font.Bold = true;
                ws4.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#7f1d1d");
                ws4.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            for (int i = 0; i < canhBaos.Count; i++)
            {
                var cb = canhBaos[i];
                var parts = (cb.GhiChu ?? "").Split('|');
                var bienSo = parts.FirstOrDefault(p => p.StartsWith("Xe:"))?.Replace("Xe:", "") ?? "--";
                var cua = parts.FirstOrDefault(p => p.StartsWith("Cua:"))?.Replace("Cua:", "") ?? "--";

                ws4.Cell(i + 2, 1).Value = i + 1;
                ws4.Cell(i + 2, 2).Value = bienSo;
                ws4.Cell(i + 2, 3).Value = cua;
                ws4.Cell(i + 2, 4).Value = cb.ThoiGian.ToString("dd/MM/yyyy HH:mm");
                ws4.Cell(i + 2, 5).Value = cb.GhiChu ?? "";
                ws4.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
            }
            ws4.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BaoCao_{from:ddMMyyyy}_{to.AddDays(-1):ddMMyyyy}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
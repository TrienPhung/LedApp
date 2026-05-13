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
        [Authorize(Policy = "BaoCao.View")]
        public IActionResult Index() => View();

        [HttpGet]
        [Authorize(Policy = "BaoCao.View")]
        public async Task<IActionResult> GetBaoCao(string? tuNgay, string? denNgay)
        {
            try
            {
                var from = string.IsNullOrEmpty(tuNgay) ? DateTime.Today.AddDays(-30) : DateTime.Parse(tuNgay);
                var to = string.IsNullOrEmpty(denNgay) ? DateTime.Today.AddDays(1) : DateTime.Parse(denNgay).AddDays(1);

                // ── NHẬP ──
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

                // Hàng hóa nhập
                var allChiNhap = nhaps.SelectMany(n => n.ChitietNhaps ?? new List<ChitietNhap>()).ToList();
                var tongDaBGNhap = allChiNhap.Sum(c => c.DaBG);
                var tongChuaBGNhap = allChiNhap.Sum(c => c.ChuaBG);
                var tongHangNhap = tongDaBGNhap + tongChuaBGNhap;

                // KPI nhập
                var nhapCoTG = nhaps
                    .Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                    .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();
                var kpiNhapTb = nhapCoTG.Any() ? Math.Round(nhapCoTG.Average(), 1) : 0;
                var kpiNhapMin = nhapCoTG.Any() ? Math.Round(nhapCoTG.Min(), 1) : 0;
                var kpiNhapMax = nhapCoTG.Any() ? Math.Round(nhapCoTG.Max(), 1) : 0;

                // ── XUẤT ──
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

                // Hàng hóa xuất
                var allChiXuat = xuats.SelectMany(x => x.ChitietXuats ?? new List<ChitietXuat>()).ToList();
                var tongDaBGXuat = allChiXuat.Sum(c => c.DaBG);
                var tongChuaBGXuat = allChiXuat.Sum(c => c.ChuaBG);
                var tongHangXuat = tongDaBGXuat + tongChuaBGXuat;

                // KPI xuất
                var xuatCoTG = xuats
                    .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                    .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                    .ToList();
                var kpiXuatTb = xuatCoTG.Any() ? Math.Round(xuatCoTG.Average(), 1) : 0;
                var kpiXuatMin = xuatCoTG.Any() ? Math.Round(xuatCoTG.Min(), 1) : 0;
                var kpiXuatMax = xuatCoTG.Any() ? Math.Round(xuatCoTG.Max(), 1) : 0;

                // ── CHART THEO NGÀY ──
                var lichSu = await _context.LichSuBanGiaos
                    .AsNoTracking()
                    .Where(l => l.ThoiGian >= from && l.ThoiGian < to)
                    .GroupBy(l => new { l.ThoiGian.Date, l.LoaiPhieu })
                    .Select(g => new { Ngay = g.Key.Date, Loai = g.Key.LoaiPhieu, SoLuot = g.Count() })
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

                // ── TOP CỬA ──
                var topCuaNhap = nhaps
                    .GroupBy(n => n.CuaNhap?.Ten ?? "Không rõ")
                    .Select(g => new
                    {
                        ten = g.Key,
                        soPhieu = g.Count(),
                        hoanThanh = g.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                        daBG = g.SelectMany(n => n.ChitietNhaps ?? new List<ChitietNhap>()).Sum(c => c.DaBG),
                        chuaBG = g.SelectMany(n => n.ChitietNhaps ?? new List<ChitietNhap>()).Sum(c => c.ChuaBG)
                    })
                    .OrderByDescending(g => g.soPhieu).Take(5).ToList();

                var topCuaXuat = xuats
                    .GroupBy(x => x.CuaXuat?.Ten ?? "Không rõ")
                    .Select(g => new
                    {
                        ten = g.Key,
                        soPhieu = g.Count(),
                        hoanThanh = g.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                        daBG = g.SelectMany(x => x.ChitietXuats ?? new List<ChitietXuat>()).Sum(c => c.DaBG),
                        chuaBG = g.SelectMany(x => x.ChitietXuats ?? new List<ChitietXuat>()).Sum(c => c.ChuaBG)
                    })
                    .OrderByDescending(g => g.soPhieu).Take(5).ToList();

                // ── ĐƠN VỊ BÀN GIAO ──
                var donViNhap = allChiNhap
                    .GroupBy(c => c.DonVi)
                    .Select(g => new
                    {
                        donVi = g.Key,
                        daBG = g.Sum(c => c.DaBG),
                        chuaBG = g.Sum(c => c.ChuaBG),
                        tongSo = g.Sum(c => c.DaBG + c.ChuaBG)
                    })
                    .OrderByDescending(g => g.tongSo).ToList();

                var donViXuat = allChiXuat
                    .GroupBy(c => c.DonVi)
                    .Select(g => new
                    {
                        donVi = g.Key,
                        daBG = g.Sum(c => c.DaBG),
                        chuaBG = g.Sum(c => c.ChuaBG),
                        tongSo = g.Sum(c => c.DaBG + c.ChuaBG)
                    })
                    .OrderByDescending(g => g.tongSo).ToList();

                // ── CHI TIẾT THEO NGÀY ──
                var chitietNhap = nhaps
                    .GroupBy(n => n.ThoiGianPhanCong.Date)
                    .Select(g => new
                    {
                        ngay = g.Key.ToString("dd/MM/yyyy"),
                        tongSo = g.Count(),
                        hoanThanh = g.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                        quaGio = g.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                        dangNhap = g.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao),
                        daBG = g.SelectMany(n => n.ChitietNhaps ?? new List<ChitietNhap>()).Sum(c => c.DaBG),
                        chuaBG = g.SelectMany(n => n.ChitietNhaps ?? new List<ChitietNhap>()).Sum(c => c.ChuaBG)
                    })
                    .OrderByDescending(g => g.ngay).ToList();

                var chitietXuat = xuats
                    .GroupBy(x => x.ThoiGianPhanCong.Date)
                    .Select(g => new
                    {
                        ngay = g.Key.ToString("dd/MM/yyyy"),
                        tongSo = g.Count(),
                        hoanThanh = g.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                        quaGio = g.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                        dangXuat = g.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                        daBG = g.SelectMany(x => x.ChitietXuats ?? new List<ChitietXuat>()).Sum(c => c.DaBG),
                        chuaBG = g.SelectMany(x => x.ChitietXuats ?? new List<ChitietXuat>()).Sum(c => c.ChuaBG)
                    })
                    .OrderByDescending(g => g.ngay).ToList();

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
                        tongHangNhap,
                        tongDaBGNhap,
                        tongChuaBGNhap,
                        tongXuat,
                        xuatHoanThanh,
                        xuatQuaGio,
                        xuatDangXuLy = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                        tongHangXuat,
                        tongDaBGXuat,
                        tongChuaBGXuat,
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
                    donViNhap,
                    donViXuat,
                    chitietNhap,
                    chitietXuat
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi GetBaoCao");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Policy = "BaoCao.Export")]
        public async Task<IActionResult> ExportExcel(string? tuNgay, string? denNgay)
        {
            var from = string.IsNullOrEmpty(tuNgay) ? DateTime.Today.AddDays(-30) : DateTime.Parse(tuNgay);
            var to = string.IsNullOrEmpty(denNgay) ? DateTime.Today.AddDays(1) : DateTime.Parse(denNgay).AddDays(1);

            var nhaps = await _context.Nhaps.AsNoTracking()
                .Include(n => n.CuaNhap).Include(n => n.ChitietNhaps)
                .Where(n => n.ThoiGianPhanCong >= from && n.ThoiGianPhanCong < to)
                .OrderByDescending(n => n.ThoiGianPhanCong).ToListAsync();

            var xuats = await _context.Xuats.AsNoTracking()
                .Include(x => x.CuaXuat).Include(x => x.Xe).Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong >= from && x.ThoiGianPhanCong < to)
                .OrderByDescending(x => x.ThoiGianPhanCong).ToListAsync();

            using var wb = new XLWorkbook();

            void StyleHeader(IXLCell c, string hex)
            {
                c.Style.Font.Bold = true;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml(hex);
                c.Style.Font.FontColor = XLColor.White;
                c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Sheet 1: Tổng hợp
            var ws1 = wb.Worksheets.Add("Tổng hợp");
            ws1.Cell("A1").Value = $"BÁO CÁO THỐNG KÊ — Từ {from:dd/MM/yyyy} đến {to.AddDays(-1):dd/MM/yyyy}";
            ws1.Cell("A1").Style.Font.Bold = true; ws1.Cell("A1").Style.Font.FontSize = 14;
            ws1.Range("A1:E1").Merge();
            new[] { "Chỉ số", "Nhập", "Xuất" }.Select((h, i) => { ws1.Cell(3, i + 1).Value = h; StyleHeader(ws1.Cell(3, i + 1), "#0f172a"); return 0; }).ToList();

            var nhKpi = nhaps.Where(n => n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue)
                .Select(n => (n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes).DefaultIfEmpty(0).ToList();
            var xuKpi = xuats.Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes).DefaultIfEmpty(0).ToList();

            var rows = new[]{
                ("Tổng phiếu", nhaps.Count.ToString(), xuats.Count.ToString()),
                ("Hoàn thành", nhaps.Count(n=>n.TrangThai==(int)TrangThaiNhap.HoanThanh).ToString(),
                    xuats.Count(x=>x.TrangThai==(int)TrangThaiXuat.HoanThanh||x.TrangThai==(int)TrangThaiXuat.DaXuatPhat).ToString()),
                ("Quá giờ", nhaps.Count(n=>n.TrangThai==(int)TrangThaiNhap.QuaThoiGian).ToString(),
                    xuats.Count(x=>x.TrangThai==(int)TrangThaiXuat.QuaThoiGian).ToString()),
                ("Tổng hàng (đv)", nhaps.SelectMany(n=>n.ChitietNhaps??new List<ChitietNhap>()).Sum(c=>c.DaBG+c.ChuaBG).ToString(),
                    xuats.SelectMany(x=>x.ChitietXuats??new List<ChitietXuat>()).Sum(c=>c.DaBG+c.ChuaBG).ToString()),
                ("Đã bàn giao", nhaps.SelectMany(n=>n.ChitietNhaps??new List<ChitietNhap>()).Sum(c=>c.DaBG).ToString(),
                    xuats.SelectMany(x=>x.ChitietXuats??new List<ChitietXuat>()).Sum(c=>c.DaBG).ToString()),
                ("Chưa bàn giao", nhaps.SelectMany(n=>n.ChitietNhaps??new List<ChitietNhap>()).Sum(c=>c.ChuaBG).ToString(),
                    xuats.SelectMany(x=>x.ChitietXuats??new List<ChitietXuat>()).Sum(c=>c.ChuaBG).ToString()),
                ("KPI Trung bình (ph)", Math.Round(nhKpi.Average(),1).ToString(), Math.Round(xuKpi.Average(),1).ToString()),
                ("KPI Nhanh nhất (ph)", Math.Round(nhKpi.Min(),1).ToString(), Math.Round(xuKpi.Min(),1).ToString()),
                ("KPI Chậm nhất (ph)", Math.Round(nhKpi.Max(),1).ToString(), Math.Round(xuKpi.Max(),1).ToString()),
            };
            for (int r = 0; r < rows.Length; r++)
            {
                ws1.Cell(r + 4, 1).Value = rows[r].Item1;
                ws1.Cell(r + 4, 2).Value = rows[r].Item2; ws1.Cell(r + 4, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws1.Cell(r + 4, 3).Value = rows[r].Item3; ws1.Cell(r + 4, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            ws1.Columns().AdjustToContents();

            // Sheet 2: Chi tiết nhập
            var ws2 = wb.Worksheets.Add("Chi tiết nhập");
            var hdN = new[] { "STT", "Biển số", "Cửa nhập", "Phân công", "Vào cửa", "Giới hạn", "Hoàn thành", "TG BG (ph)", "Đã BG", "Chưa BG", "Trạng thái" };
            for (int i = 0; i < hdN.Length; i++) { ws2.Cell(1, i + 1).Value = hdN[i]; StyleHeader(ws2.Cell(1, i + 1), "#1e3a5f"); }
            var ttN = new Dictionary<int, string> { { 0, "Đã phân công" }, { 1, "Đang bàn giao" }, { 2, "Quá giờ" }, { 3, "Hoàn thành" } };
            for (int i = 0; i < nhaps.Count; i++)
            {
                var n = nhaps[i];
                double tg = (n.ThoiGianVaoCua.HasValue && n.ThoiGianHoanThanh.HasValue) ? Math.Round((n.ThoiGianHoanThanh!.Value - n.ThoiGianVaoCua!.Value).TotalMinutes, 1) : 0;
                ws2.Cell(i + 2, 1).Value = i + 1; ws2.Cell(i + 2, 2).Value = n.BienSoXe; ws2.Cell(i + 2, 3).Value = n.CuaNhap?.Ten ?? "--";
                ws2.Cell(i + 2, 4).Value = n.ThoiGianPhanCong.ToString("dd/MM/yyyy HH:mm");
                ws2.Cell(i + 2, 5).Value = n.ThoiGianVaoCua?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 6).Value = n.ThoiGianGioiHan?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 7).Value = n.ThoiGianHoanThanh?.ToString("HH:mm") ?? "--";
                ws2.Cell(i + 2, 8).Value = tg > 0 ? tg.ToString() : "--";
                ws2.Cell(i + 2, 9).Value = n.ChitietNhaps?.Sum(c => c.DaBG) ?? 0;
                ws2.Cell(i + 2, 10).Value = n.ChitietNhaps?.Sum(c => c.ChuaBG) ?? 0;
                ws2.Cell(i + 2, 11).Value = ttN.GetValueOrDefault(n.TrangThai, "--");
                if (n.TrangThai == (int)TrangThaiNhap.QuaThoiGian) ws2.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
                else if (n.TrangThai == (int)TrangThaiNhap.HoanThanh) ws2.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fff4");
            }
            ws2.Columns().AdjustToContents();

            // Sheet 3: Chi tiết xuất
            var ws3 = wb.Worksheets.Add("Chi tiết xuất");
            var hdX = new[] { "STT", "Biển số", "Cửa xuất", "Phân công", "Vào cửa", "Giới hạn", "Hoàn thành", "TG BG (ph)", "Đã BG", "Chưa BG", "Trạng thái" };
            for (int i = 0; i < hdX.Length; i++) { ws3.Cell(1, i + 1).Value = hdX[i]; StyleHeader(ws3.Cell(1, i + 1), "#0f6e56"); }
            var ttX = new Dictionary<int, string> { { 0, "Đã phân công" }, { 1, "Đang bàn giao" }, { 2, "Quá giờ" }, { 3, "Hoàn thành" }, { 4, "Đã xuất phát" } };
            for (int i = 0; i < xuats.Count; i++)
            {
                var x = xuats[i];
                double tg = (x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue) ? Math.Round((x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes, 1) : 0;
                ws3.Cell(i + 2, 1).Value = i + 1; ws3.Cell(i + 2, 2).Value = x.Xe?.BienSoXe ?? "--"; ws3.Cell(i + 2, 3).Value = x.CuaXuat?.Ten ?? "--";
                ws3.Cell(i + 2, 4).Value = x.ThoiGianPhanCong.ToString("dd/MM/yyyy HH:mm");
                ws3.Cell(i + 2, 5).Value = x.ThoiGianVaoCua?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 6).Value = x.ThoiGianGioiHan?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 7).Value = x.ThoiGianHoanThanh?.ToString("HH:mm") ?? "--";
                ws3.Cell(i + 2, 8).Value = tg > 0 ? tg.ToString() : "--";
                ws3.Cell(i + 2, 9).Value = x.ChitietXuats?.Sum(c => c.DaBG) ?? 0;
                ws3.Cell(i + 2, 10).Value = x.ChitietXuats?.Sum(c => c.ChuaBG) ?? 0;
                ws3.Cell(i + 2, 11).Value = ttX.GetValueOrDefault(x.TrangThai, "--");
                if (x.TrangThai == (int)TrangThaiXuat.QuaThoiGian) ws3.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
                else if (x.TrangThai == (int)TrangThaiXuat.HoanThanh || x.TrangThai == (int)TrangThaiXuat.DaXuatPhat) ws3.Row(i + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fff4");
            }
            ws3.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream); stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BaoCao_{from:ddMMyyyy}_{to.AddDays(-1):ddMMyyyy}.xlsx");
        }
    }
}
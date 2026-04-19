using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Data;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

namespace LedApp.Controllers
{
    [Authorize]
    public class BaoCaoController : Controller
    {
        private readonly ApplicationDBContext _context;

        public BaoCaoController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: /BaoCao
        public IActionResult Index()
        {
            return View();
        }

        // GET: /BaoCao/GetBaoCaoXuat?ngay=2024-01-15
        [HttpGet]
        public async Task<IActionResult> GetBaoCaoXuat(string? ngay)
        {
            var date = string.IsNullOrEmpty(ngay)
                ? DateTime.Today
                : DateTime.Parse(ngay);

            var xuats = await _context.Xuats
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.CuaXuat)
                .Include(x => x.NhanVienXacNhan)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == date)
                .OrderBy(x => x.ThoiGianPhanCong)
                .ToListAsync();

            // ── Tổng hợp ──
            var tongXe = xuats.Count;
            var xeHoanThanh = xuats.Count(x => x.TrangThai >= (int)TrangThaiXuat.HoanThanh);
            var xeDaRoi = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaXuatPhat);
            var xeQuaGio = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian
                                               || x.ThoiGianHoanThanh > x.ThoiGianGioiHan);

            // KPI: thời gian bàn giao trung bình (phút)
            var kpiList = xuats
                .Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                .ToList();
            var kpiTrungBinh = kpiList.Any() ? Math.Round(kpiList.Average(), 1) : 0;
            var kpiNhanh = kpiList.Any() ? Math.Round(kpiList.Min(), 1) : 0;
            var kpiCham = kpiList.Any() ? Math.Round(kpiList.Max(), 1) : 0;

            // Tổng hàng hóa
            var tongHang = new Dictionary<string, long>();
            foreach (var x in xuats.Where(x => x.TrangThai >= (int)TrangThaiXuat.HoanThanh))
            {
                foreach (var c in x.ChitietXuats ?? new List<ChitietXuat>())
                {
                    if (!tongHang.ContainsKey(c.DonVi)) tongHang[c.DonVi] = 0;
                    tongHang[c.DonVi] += c.DaBG;
                }
            }

            // Chi tiết từng xe
            var chiTiet = xuats.Select(x =>
            {
                var tgBanGiao = x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue
                    ? Math.Round((x.ThoiGianHoanThanh.Value - x.ThoiGianVaoCua.Value).TotalMinutes, 1)
                    : (double?)null;
                var quaGio = x.ThoiGianHoanThanh.HasValue && x.ThoiGianGioiHan.HasValue
                    && x.ThoiGianHoanThanh > x.ThoiGianGioiHan;

                return new
                {
                    x.Id,
                    BienSoXe = x.Xe?.BienSoXe ?? "--",
                    LoaiXe = x.Xe?.LoaiXe ?? "--",
                    TenTaiXe = x.Xe?.TaiXe?.FullName ?? "--",
                    TenCua = x.CuaXuat?.Ten ?? "--",
                    TenNhanVien = x.NhanVienXacNhan?.FullName ?? "--",
                    x.TrangThai,
                    TrangThaiText = TrangThaiText(x.TrangThai),
                    x.ThoiGianPhanCong,
                    x.ThoiGianVaoCua,
                    x.ThoiGianHoanThanh,
                    x.ThoiGianXuatPhat,
                    x.ThoiGianGioiHan,
                    TgBanGiaoPhut = tgBanGiao,
                    QuaGio = quaGio,
                    HangHoas = x.ChitietXuats?.Select(c => new
                    {
                        c.DonVi,
                        c.ChuaBG,
                        c.DaBG
                    }).ToList()
                };
            }).ToList();

            return Json(new
            {
                Ngay = date.ToString("dd/MM/yyyy"),
                TongXe = tongXe,
                XeHoanThanh = xeHoanThanh,
                XeDaRoi = xeDaRoi,
                XeQuaGio = xeQuaGio,
                KpiTrungBinh = kpiTrungBinh,
                KpiNhanh = kpiNhanh,
                KpiCham = kpiCham,
                TongHang = tongHang,
                ChiTiet = chiTiet
            });
        }

        // GET: /BaoCao/GetLichSuBanGiao?ngay=2024-01-15
        [HttpGet]
        public async Task<IActionResult> GetLichSuBanGiao(string? ngay)
        {
            var date = string.IsNullOrEmpty(ngay)
                ? DateTime.Today
                : DateTime.Parse(ngay);

            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .Where(l => l.ThoiGian.Date == date && l.LoaiPhieu == "XUAT")
                .OrderByDescending(l => l.ThoiGian)
                .Select(l => new
                {
                    l.Id,
                    l.LoaiPhieu,
                    l.PhieuId,
                    l.DonVi,
                    l.SoBG,
                    TenNhanVien = l.NhanVien != null ? l.NhanVien.FullName : "--",
                    ThoiGian = l.ThoiGian.ToString("HH:mm:ss")
                })
                .ToListAsync();

            return Json(lichSu);
        }

        // GET: /BaoCao/XuatExcel?ngay=2024-01-15
        [HttpGet]
        public async Task<IActionResult> XuatExcel(string? ngay)
        {
            var date = string.IsNullOrEmpty(ngay)
                ? DateTime.Today
                : DateTime.Parse(ngay);

            var xuats = await _context.Xuats
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.CuaXuat)
                .Include(x => x.NhanVienXacNhan)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == date)
                .OrderBy(x => x.ThoiGianPhanCong)
                .ToListAsync();

            var lichSu = await _context.LichSuBanGiaos
                .Include(l => l.NhanVien)
                .Where(l => l.ThoiGian.Date == date && l.LoaiPhieu == "XUAT")
                .OrderBy(l => l.ThoiGian)
                .ToListAsync();

            using var wb = new XLWorkbook();

            // ══════════════════════════════════════
            // SHEET 1: TỔNG HỢP
            // ══════════════════════════════════════
            var ws1 = wb.Worksheets.Add("Tổng hợp");

            // Tiêu đề
            ws1.Cell("A1").Value = $"BÁO CÁO XUẤT KHO — NGÀY {date:dd/MM/yyyy}";
            ws1.Cell("A1").Style.Font.Bold = true;
            ws1.Cell("A1").Style.Font.FontSize = 16;
            ws1.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;
            ws1.Range("A1:H1").Merge();
            ws1.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws1.Cell("A2").Value = $"Xuất lúc: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws1.Cell("A2").Style.Font.Italic = true;
            ws1.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            ws1.Range("A2:H2").Merge();
            ws1.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // KPI boxes
            var kpiRow = 4;
            var kpiHeaders = new[] { "Tổng xe", "Hoàn thành", "Đã rời kho", "Quá giờ", "TG BG TB (phút)", "Nhanh nhất", "Chậm nhất" };
            var kpiColors = new[] { XLColor.SteelBlue, XLColor.SeaGreen, XLColor.DarkGreen, XLColor.Red, XLColor.DarkOrange, XLColor.ForestGreen, XLColor.DarkRed };

            for (int i = 0; i < kpiHeaders.Length; i++)
            {
                var col = (char)('A' + i);
                ws1.Cell($"{col}{kpiRow}").Value = kpiHeaders[i];
                ws1.Cell($"{col}{kpiRow}").Style.Font.Bold = true;
                ws1.Cell($"{col}{kpiRow}").Style.Font.FontColor = XLColor.White;
                ws1.Cell($"{col}{kpiRow}").Style.Fill.BackgroundColor = kpiColors[i];
                ws1.Cell($"{col}{kpiRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var kpiData = new[] {
                (double)xuats.Count,
                (double)xuats.Count(x => x.TrangThai >= (int)TrangThaiXuat.HoanThanh),
                (double)xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                (double)xuats.Count(x => x.ThoiGianHoanThanh > x.ThoiGianGioiHan),
                xuats.Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                     .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                     .DefaultIfEmpty(0).Average(),
                xuats.Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                     .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                     .DefaultIfEmpty(0).Min(),
                xuats.Where(x => x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue)
                     .Select(x => (x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes)
                     .DefaultIfEmpty(0).Max()
            };

            for (int i = 0; i < kpiData.Length; i++)
            {
                var col = (char)('A' + i);
                ws1.Cell($"{col}{kpiRow + 1}").Value = Math.Round(kpiData[i], 1);
                ws1.Cell($"{col}{kpiRow + 1}").Style.Font.Bold = true;
                ws1.Cell($"{col}{kpiRow + 1}").Style.Font.FontSize = 13;
                ws1.Cell($"{col}{kpiRow + 1}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Bảng chi tiết
            var tblRow = kpiRow + 4;
            var headers1 = new[] { "STT", "Biển số", "Loại xe", "Tài xế", "Cửa", "NV Xác nhận", "Trạng thái", "Giờ phân công", "Giờ vào cửa", "Giờ hoàn thành", "Giờ rời kho", "TG BG (phút)", "Quá giờ" };

            for (int i = 0; i < headers1.Length; i++)
            {
                var cell = ws1.Cell(tblRow, i + 1);
                cell.Value = headers1[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int stt = 1;
            foreach (var x in xuats)
            {
                var r = tblRow + stt;
                var tgBG = x.ThoiGianVaoCua.HasValue && x.ThoiGianHoanThanh.HasValue
                    ? Math.Round((x.ThoiGianHoanThanh!.Value - x.ThoiGianVaoCua!.Value).TotalMinutes, 1)
                    : (double?)null;
                var quaGio = x.ThoiGianHoanThanh.HasValue && x.ThoiGianGioiHan.HasValue
                    && x.ThoiGianHoanThanh > x.ThoiGianGioiHan;

                var vals = new object?[]
                {
                    stt,
                    x.Xe?.BienSoXe ?? "--",
                    x.Xe?.LoaiXe   ?? "--",
                    x.Xe?.TaiXe?.FullName ?? "--",
                    x.CuaXuat?.Ten ?? "--",
                    x.NhanVienXacNhan?.FullName ?? "--",
                    TrangThaiText(x.TrangThai),
                    x.ThoiGianPhanCong.ToString("HH:mm"),
                    x.ThoiGianVaoCua?.ToString("HH:mm") ?? "--",
                    x.ThoiGianHoanThanh?.ToString("HH:mm") ?? "--",
                    x.ThoiGianXuatPhat?.ToString("HH:mm") ?? "--",
                    tgBG.HasValue ? tgBG : "--",
                    quaGio ? "CÓ" : "Không"
                };

                for (int c = 0; c < vals.Length; c++)
                {
                    var cell = ws1.Cell(r, c + 1);
                    cell.Value = vals[c]?.ToString() ?? "--";
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (quaGio)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#fff0f0");
                }
                stt++;
            }

            // Auto-fit columns
            ws1.Columns().AdjustToContents();

            // ══════════════════════════════════════
            // SHEET 2: CHI TIẾT BÀN GIAO
            // ══════════════════════════════════════
            var ws2 = wb.Worksheets.Add("Chi tiết bàn giao");

            ws2.Cell("A1").Value = $"LỊCH SỬ BÀN GIAO XUẤT KHO — {date:dd/MM/yyyy}";
            ws2.Cell("A1").Style.Font.Bold = true;
            ws2.Cell("A1").Style.Font.FontSize = 14;
            ws2.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;
            ws2.Range("A1:F1").Merge();
            ws2.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var headers2 = new[] { "STT", "Phiếu #", "Đơn vị", "Số lượng BG", "Nhân viên", "Thời gian" };
            for (int i = 0; i < headers2.Length; i++)
            {
                var cell = ws2.Cell(3, i + 1);
                cell.Value = headers2[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0f2a1a");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row2 = 4;
            foreach (var l in lichSu)
            {
                var vals2 = new object[] { row2 - 3, l.PhieuId, l.DonVi, l.SoBG, l.NhanVien?.FullName ?? "--", l.ThoiGian.ToString("HH:mm:ss") };
                for (int c = 0; c < vals2.Length; c++)
                {
                    var cell = ws2.Cell(row2, c + 1);
                    cell.Value = vals2[c].ToString() ?? "--";
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    if (row2 % 2 == 0)
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fffe");
                }
                row2++;
            }

            // Tổng cộng
            ws2.Cell(row2, 1).Value = "TỔNG";
            ws2.Cell(row2, 4).FormulaA1 = $"=SUM(D4:D{row2 - 1})";
            ws2.Range(row2, 1, row2, 6).Style.Font.Bold = true;
            ws2.Range(row2, 1, row2, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8fff4");

            ws2.Columns().AdjustToContents();

            // Xuất file
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BaoCaoXuat_{date:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string TrangThaiText(int tt) => tt switch
        {
            0 => "Chờ vào cửa",
            1 => "Đang bàn giao",
            2 => "Quá thời gian",
            3 => "Hoàn thành",
            4 => "Đã rời kho",
            _ => "--"
        };
    }
}
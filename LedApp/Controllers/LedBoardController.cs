using LedApp.Data;
using LedApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    public class LedBoardController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public LedBoardController(ApplicationDBContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Nhap() => View();
        public IActionResult Xuat() => View();

        [HttpGet]
        public async Task<IActionResult> GetNhapBoard()
        {
            var nhaps = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .Where(n => n.TrangThai != (int)TrangThaiNhap.HoanThanh)
                .ToListAsync();

            var bienSoDangXuLy = nhaps.Select(n => n.BienSoXe).ToHashSet();

            var xeList = new List<LedApp.DTOs.DanhSachXeDto>();
            var xeDict = new Dictionary<string, LedApp.DTOs.DanhSachXeDto>();
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var res = await client.GetAsync("api/DanhSachXes/dashboard");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    xeList = JsonConvert.DeserializeObject<List<LedApp.DTOs.DanhSachXeDto>>(json) ?? new();
                    foreach (var xe in xeList)
                        if (!string.IsNullOrEmpty(xe.BienSo))
                            xeDict[xe.BienSo] = xe;
                }
            }
            catch { }

            var result = new List<object>();

            // ── NHÓM 1: Xe đã phân công — sort theo ThoiGianPhanCong tăng dần
            foreach (var n in nhaps.OrderBy(n => n.ThoiGianPhanCong))
            {
                xeDict.TryGetValue(n.BienSoXe ?? "", out var xe);
                result.Add(new
                {
                    nhapId = n.Id,
                    tenCua = n.CuaNhap?.Ten ?? "--",
                    bienSoXe = n.BienSoXe ?? "--",
                    tenLaiXe = xe?.TenLaiXe ?? "--",
                    maChiNhanh = xe?.MaChiNhanh ?? "--",
                    trangThai = n.TrangThai,
                    thoiGianPhanCong = n.ThoiGianPhanCong,   // ← thêm
                    thoiGianVaoBai = n.ThoiGianVaoBai,     // ← thêm
                    thoiGianVaoCua = n.ThoiGianVaoCua,
                    thoiGianGioiHan = n.ThoiGianGioiHan,
                    ghiChu = n.GhiChu ?? ""
                });
            }

            // ── NHÓM 2: Xe đã đến bãi, chờ phân công
            var xeDaDen = xeList
                .Where(x => x.ChuyenHienTai != null
                         && x.ChuyenHienTai.TrangThai == 2
                         && !bienSoDangXuLy.Contains(x.BienSo))
                .ToList();

            foreach (var xe in xeDaDen)
            {
                result.Add(new
                {
                    nhapId = -(xe.Id),
                    tenCua = "—",
                    bienSoXe = xe.BienSo,
                    tenLaiXe = xe.TenLaiXe ?? "--",
                    maChiNhanh = xe.MaChiNhanh ?? "--",
                    trangThai = -2,
                    thoiGianPhanCong = (DateTime?)null,
                    thoiGianVaoBai = (DateTime?)null,
                    thoiGianVaoCua = (DateTime?)null,
                    thoiGianGioiHan = (DateTime?)null,
                    ghiChu = ""
                });
            }

            // ── NHÓM 3: Đang trên đường về
            var xeDangVe = xeList
                .Where(x => x.ChuyenHienTai != null
                         && x.ChuyenHienTai.TrangThai == 1
                         && !bienSoDangXuLy.Contains(x.BienSo))
                .ToList();

            foreach (var xe in xeDangVe)
            {
                result.Add(new
                {
                    nhapId = -(xe.Id + 10000),
                    tenCua = "—",
                    bienSoXe = xe.BienSo,
                    tenLaiXe = xe.TenLaiXe ?? "--",
                    maChiNhanh = xe.MaChiNhanh ?? "--",
                    trangThai = -3,
                    thoiGianPhanCong = (DateTime?)null,
                    thoiGianVaoBai = (DateTime?)null,
                    thoiGianVaoCua = (DateTime?)null,
                    thoiGianGioiHan = (DateTime?)null,
                    ghiChu = ""
                });
            }

            return Json(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetXuatBoard()
        {
            var today = DateTime.Today;

            var trangThaiHienThi = new[]
            {
                (int)TrangThaiXuat.DaPhanCong,   // 0 - chờ xuất
                (int)TrangThaiXuat.DangBanGiao,  // 1 - đang xuất
                (int)TrangThaiXuat.QuaThoiGian,  // 2 - quá giờ
                (int)TrangThaiXuat.HoanThanh     // 3 - hoàn thành
                // KHÔNG lấy DaXuatPhat (4) — xe đã ra khỏi kho
            };

            // ── NHÓM 1: Xe đã được phân công hôm nay
            var xuats = await _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe).ThenInclude(xe => xe!.TaiXe)
                .Include(x => x.ChitietXuats)
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && trangThaiHienThi.Contains(x.TrangThai))
                .OrderBy(x => x.TrangThai)
                .ThenBy(x => x.ThoiGianPhanCong)
                .ToListAsync();

            // Biển số xe đã được phân công
            var xeIdDaPhanCong = xuats.Select(x => x.XeId).ToHashSet();

            // ── NHÓM 2: Xe trong bãi chưa được phân công
            var xeTrongBaiChuaPhanCong = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai
                         && !xeIdDaPhanCong.Contains(x.Id))
                .ToListAsync();

            var result = new List<object>();

            // ── NHÓM 1: Xe đã phân công (trừ HoanThanh để tránh lộn xộn)
            foreach (var x in xuats.Where(x => x.TrangThai != (int)TrangThaiXuat.HoanThanh))
            {
                result.Add(new
                {
                    xuatId = x.Id,
                    tenCua = x.CuaXuat?.Ten ?? "--",
                    bienSoXe = x.Xe?.BienSoXe ?? "--",
                    tenLaiXe = x.Xe?.TaiXe?.FullName ?? "--",
                    diaDiem = x.DiaDiemGiao ?? "",
                    hangHoas = x.ChitietXuats?.Select(c => new { c.DonVi, c.ChuaBG, c.DaBG }).ToList(),
                    trangThai = x.TrangThai,
                    thoiGianPhanCong = x.ThoiGianPhanCong,
                    thoiGianVaoCua = x.ThoiGianVaoCua,
                    thoiGianGioiHan = x.ThoiGianGioiHan,      // ← đã có trong model Xuat
                    thoiGianHoanThanh = x.ThoiGianHoanThanh,
                    ghiChu = x.GhiChu ?? ""
                });
            }

            // ── NHÓM 2: Xe trong bãi chờ phân công
            foreach (var xe in xeTrongBaiChuaPhanCong)
            {
                result.Add(new
                {
                    xuatId = -(xe.Id),
                    tenCua = "—",
                    bienSoXe = xe.BienSoXe,
                    tenLaiXe = xe.TaiXe?.FullName ?? "--",
                    maChiNhanh = (string?)null,
                    diaDiem = "",
                    hangHoas = new List<object>(),
                    trangThai = -2,
                    thoiGianPhanCong = (DateTime?)null,
                    thoiGianVaoCua = (DateTime?)null,
                    thoiGianGioiHan = (DateTime?)null,
                    thoiGianHoanThanh = (DateTime?)null,
                    ghiChu = ""
                });
            }

            return Json(result);
        }
    }
}
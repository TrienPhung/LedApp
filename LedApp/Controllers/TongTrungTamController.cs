using LedApp.Data;
using LedApp.DTOs;
using LedApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace LedApp.Controllers
{
    public class TongTrungTamController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public TongTrungTamController(ApplicationDBContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            ViewBag.CuaNhaps = _context.CuaNhaps.ToList();
            ViewBag.CuaXuats = _context.CuaXuats.ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSnapshot()
        {
            var today = DateTime.Today;

            // ── XUẤT ──
            var xuats = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today)
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe).ThenInclude(xe => xe.TaiXe)
                .Include(x => x.ChitietXuats)
                .ToListAsync();

            var cuaXuatList = await _context.CuaXuats.ToListAsync();
            var xuatCards = cuaXuatList.Select(cua =>
            {
                var px = xuats.FirstOrDefault(x =>
                    x.CuaXuatId == cua.Id &&
                    x.TrangThai != (int)TrangThaiXuat.DaXuatPhat);
                return new
                {
                    cuaId = cua.Id,
                    tenCua = cua.Ten,
                    isActive = cua.IsActive,
                    phieu = px == null ? null : (object)new
                    {
                        xuatId = px.Id,
                        bienSo = px.Xe?.BienSoXe ?? "--",
                        tenTaiXe = px.Xe?.TaiXe?.FullName ?? "--",
                        trangThai = px.TrangThai,
                        thoiGianVaoCua = px.ThoiGianVaoCua,
                        thoiGianGioiHan = px.ThoiGianGioiHan,
                        hangHoas = px.ChitietXuats?.Select(c => new
                        {
                            donVi = c.DonVi,
                            chuaBG = c.ChuaBG,
                            daBG = c.DaBG
                        }).ToList()
                    }
                };
            }).ToList();

            var xuatStats = new
            {
                choPhanCong = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaPhanCong),
                dangBanGiao = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao),
                quaGio = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                hoanThanh = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh),
                daRoiKho = xuats.Count(x => x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                tongHom = xuats.Count
            };

            var xeChoRaCong = xuats
                .Where(x => x.TrangThai == (int)TrangThaiXuat.HoanThanh)
                .Select(x => new
                {
                    xuatId = x.Id,
                    bienSo = x.Xe?.BienSoXe ?? "--",
                    loaiXe = x.Xe?.LoaiXe ?? "--",
                    tenTaiXe = x.Xe?.TaiXe?.FullName ?? "--",
                    tenCua = x.CuaXuat?.Ten ?? "--",
                    thoiGianHoanThanh = x.ThoiGianHoanThanh,
                    hangHoas = x.ChitietXuats?.Select(c => new
                    {
                        donVi = c.DonVi,
                        chuaBG = c.ChuaBG,
                        daBG = c.DaBG
                    }).ToList()
                }).ToList();

            // ── XE TRONG BÃI — đầy đủ thông tin ──
            var xeTrongBai = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .Select(x => new
                {
                    xeId = x.Id,
                    bienSo = x.BienSoXe,
                    loaiXe = x.LoaiXe,
                    taiTrong = x.TaiTrong,
                    tenTaiXe = x.TaiXe != null ? x.TaiXe.FullName : "--",
                    trangThai = x.TrangThai
                }).ToListAsync();

            var xeGroups = xeTrongBai
                .GroupBy(x => x.trangThai)
                .Select(g => new { trangThai = g.Key, soLuong = g.Count() })
                .ToList();

            // ── NHẬP (API Viettel) ──
            object nhapData = new
            {
                sapVeXe = 0,
                daVeXe = 0,
                chuaVaoXe = 0,
                dangBanGiao = 0,
                quaGio = 0,
                tongHom = 0,
                hoanThanh = 0,
                cards = new List<object>(),
                xeSapVe = new List<object>()
            };

            // Xe vận chuyển từ Viettel API (đang trên đường về)
            var xeVanChuyenViettel = new List<object>();

            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json) ?? new();

                    var sapVe = xes.Where(x => x.ChuyenHienTai?.TrangThai == 1).ToList();
                    var daVe = xes.Where(x => x.ChuyenHienTai?.TrangThai == 2).ToList();

                    // Xe đang trên đường về — đầy đủ thông tin
                    xeVanChuyenViettel = sapVe.Select(x => (object)new
                    {
                        bienSo = x.BienSo,
                        tenLaiXe = x.TenLaiXe ?? "--",
                        maChiNhanh = x.MaChiNhanh ?? "--",
                        maChuyenApi = x.ChuyenHienTai?.MaChuyenApi ?? "--",
                        lanThu = x.ChuyenHienTai?.LanThu ?? 0,
                        ngayDuKien = x.ChuyenHienTai?.NgayDuKien,
                        hangHoas = x.ChuyenHienTai?.HangHoas?.Select(h => new
                        {
                            donVi = h.DonVi,
                            soLuong = h.SoLuong
                        }).ToList()
                    }).ToList();

                    var nhapPhanCong = await _context.Nhaps
                        .Where(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong ||
                                    n.TrangThai == (int)TrangThaiNhap.DangBanGiao ||
                                    n.TrangThai == (int)TrangThaiNhap.QuaThoiGian)
                        .Include(n => n.CuaNhap)
                        .Include(n => n.ChitietNhaps)
                        .ToListAsync();

                    var bienSoDaPhanCong = nhapPhanCong.Select(n => n.BienSoXe).ToHashSet();
                    var bienSoChuaVao = nhapPhanCong
                        .Where(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong)
                        .Select(n => n.BienSoXe).ToList();

                    var cuaNhapList = await _context.CuaNhaps.ToListAsync();
                    var nhapCards = cuaNhapList.Select(cua =>
                    {
                        var n = nhapPhanCong.FirstOrDefault(x => x.CuaNhapId == cua.Id);
                        return (object)new
                        {
                            cuaId = cua.Id,
                            tenCua = cua.Ten,
                            phieu = n == null ? null : (object)new
                            {
                                nhapId = n.Id,
                                bienSo = n.BienSoXe,
                                trangThai = n.TrangThai,
                                thoiGianVaoCua = n.ThoiGianVaoCua,
                                thoiGianGioiHan = n.ThoiGianGioiHan,
                                hangHoas = n.ChitietNhaps?.Select(c => new
                                {
                                    donVi = c.DonVi,
                                    chuaBG = c.ChuaBG,
                                    daBG = c.DaBG
                                }).ToList()
                            }
                        };
                    }).ToList();

                    var nhapHomNay = await _context.Nhaps
                        .Where(n => n.ThoiGianPhanCong.Date == today)
                        .ToListAsync();

                    nhapData = new
                    {
                        sapVeXe = sapVe.Count,
                        daVeXe = daVe.Count,
                        chuaVaoXe = bienSoChuaVao.Count,
                        dangBanGiao = nhapPhanCong.Count(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao),
                        quaGio = nhapPhanCong.Count(n => n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                        tongHom = nhapHomNay.Count,
                        hoanThanh = nhapHomNay.Count(n => n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                        cards = nhapCards,
                        xeSapVe = sapVe.Take(8).Select(x => new
                        {
                            bienSo = x.BienSo,
                            tenLaiXe = x.TenLaiXe,
                            daPhanCong = bienSoDaPhanCong.Contains(x.BienSo),
                            ngayDuKien = x.ChuyenHienTai?.NgayDuKien,
                            hangHoas = x.ChuyenHienTai?.HangHoas?.Select(h => new
                            {
                                donVi = h.DonVi,
                                soLuong = h.SoLuong
                            }).ToList()
                        }).ToList<object>()
                    };
                }
            }
            catch { /* API offline */ }

            // ── CẢNH BÁO — fix parse GhiChu ──
            var canhBaos = await _context.CanhBaos
                .Where(c => c.TrangThai == 0 || c.TrangThai == 1)
                .OrderByDescending(c => c.ThoiGian)
                .Take(10)
                .ToListAsync();

            var canhBaoList = canhBaos.Select(c =>
            {
                // Fix: chỉ split theo ':' lần đầu tiên để tránh cắt biển số có dấu '-'
                var parts = new Dictionary<string, string>();
                foreach (var p in (c.GhiChu ?? "").Split('|'))
                {
                    var idx = p.IndexOf(':');
                    if (idx > 0)
                        parts[p[..idx].Trim()] = p[(idx + 1)..].Trim();
                }

                // QuaHanNhap:  Xe:30B-67890|Cua:6|GioiHan:21:07
                // BaoSaiXe:    DB:29KT-123|ThucTe:51A-999|Cua:1
                return new
                {
                    id = c.Id,
                    trangThai = c.TrangThai,
                    thoiGian = c.ThoiGian,
                    loaiCanhBao = c.LoaiCanhBao,
                    bienSoDB = parts.GetValueOrDefault("DB",
                                  parts.GetValueOrDefault("Xe", "--")),
                    bienSoTT = parts.GetValueOrDefault("ThucTe", "--"),
                    cuaId = parts.GetValueOrDefault("Cua", "--"),
                    gioiHan = parts.GetValueOrDefault("GioiHan", "--")
                };
            }).ToList();

            // ── KPI ──
            var kpi = new
            {
                tongNhapHom = await _context.Nhaps.CountAsync(n => n.ThoiGianPhanCong.Date == today),
                tongXuatHom = await _context.Xuats.CountAsync(x => x.ThoiGianPhanCong.Date == today),
                nhapHoanThanh = await _context.Nhaps.CountAsync(n => n.ThoiGianPhanCong.Date == today && n.TrangThai == (int)TrangThaiNhap.HoanThanh),
                xuatHoanThanh = await _context.Xuats.CountAsync(x => x.ThoiGianPhanCong.Date == today && x.TrangThai == (int)TrangThaiXuat.DaXuatPhat),
                nhapQuaGio = await _context.Nhaps.CountAsync(n => n.ThoiGianPhanCong.Date == today && n.TrangThai == (int)TrangThaiNhap.QuaThoiGian),
                xuatQuaGio = await _context.Xuats.CountAsync(x => x.ThoiGianPhanCong.Date == today && x.TrangThai == (int)TrangThaiXuat.QuaThoiGian),
                xeGroups
            };
            var xeRoiKho = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today
                         && x.TrangThai == (int)TrangThaiXuat.DaXuatPhat)
                .Include(x => x.Xe)
                .Include(x => x.CuaXuat)
                .OrderByDescending(x => x.ThoiGianXuatPhat)
                .Select(x => new
                {
                    bienSo = x.Xe != null ? x.Xe.BienSoXe : "--",
                    tenCua = x.CuaXuat != null ? x.CuaXuat.Ten : "--",
                    thoiGianRoi = x.ThoiGianXuatPhat
                })
                .ToListAsync();
            return Ok(new
            {
                timestamp = DateTime.Now,
                nhap = nhapData,
                xuat = new { stats = xuatStats, cards = xuatCards },
                xeChoRaCong,
                xeVanChuyenViettel, // xe đang trên đường về (từ API Viettel)
                xeTrongBai,         // xe trong bãi đầy đủ thông tin
                xeRoiKho,
                canhBaos = canhBaoList,
                kpi
            });
        }
    }
}
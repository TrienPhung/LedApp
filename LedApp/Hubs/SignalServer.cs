using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Repositories;
using Microsoft.AspNetCore.SignalR;
using LedApp.Data;
using LedApp.DTOs;
using System.Net.Http;
using Newtonsoft.Json;

namespace LedApp.Hubs
{
    public class SignalServer : Hub
    {
        private readonly ApplicationDBContext dbContext;
        private readonly ChitietXuatRepository xerepo;
        private readonly XuatRepository xrepo;
        private readonly CuaXuatRepository cuaXuatRepo;
        private readonly NhapRepository nhapRepo;
        private readonly ChitietNhapRepository chitietnhapRepo;
        private readonly CuaNhapRepository cuaNhapRepo;
        private readonly IHttpClientFactory _httpClientFactory;

        public SignalServer(ApplicationDBContext dbContext, IHttpClientFactory httpClientFactory)
        {
            this.dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            xerepo = new ChitietXuatRepository(dbContext);
            xrepo = new XuatRepository(dbContext);
            cuaXuatRepo = new CuaXuatRepository(dbContext);
            cuaNhapRepo = new CuaNhapRepository(dbContext);
            chitietnhapRepo = new ChitietNhapRepository(dbContext);
            nhapRepo = new NhapRepository(dbContext);
        }

        // ==================== XUẤT ====================

        //// Lấy phiếu xuất theo cửa
        //public async Task Sendxuat(int? id)
        //{
        //    var x = xrepo.GetXuat(id);
        //    if (Clients != null)
        //        await Clients.All.SendAsync("Receivedxuat", x, id);
        //}

        //// Lấy chi tiết hàng hóa theo cửa
        //public async Task Sendxexuat(int id)
        //{
        //    var x = xrepo.GetXuat(id);
        //    if (x == null)
        //    {
        //        // Kiểm tra Clients trước khi gọi
        //        if (Clients != null)
        //            await Clients.All.SendAsync("Receivedxexuat", null, id);
        //        return;
        //    }
        //    var xexuat = xerepo.GetChitietXuat(x.Id);
        //    if (Clients != null)
        //        await Clients.All.SendAsync("Receivedxexuat", xexuat, id);
        //}
        // ===== THAY THẾ 2 HÀM TRONG SignalServer.cs =====

        // Lấy phiếu xuất theo cửa — trả DTO có BienSoXe
        public async Task Sendxuat(int? id)
        {
            var x = xrepo.GetXuat(id);  // đã Include(x => x.Xe)
            if (x == null)
            {
                await Clients.All.SendAsync("Receivedxuat", null, id);
                return;
            }

            // FIX: trả DTO thay vì model — tránh circular reference và null navigation
            var dto = new
            {
                id = x.Id,
                cuaXuatId = x.CuaXuatId,
                xeId = x.XeId,
                bienSoXe = x.Xe?.BienSoXe ?? "--",   // ← lấy từ navigation
                trangThai = x.TrangThai,
                thoiGianVaoCua = x.ThoiGianVaoCua,
                thoiGianGioiHan = x.ThoiGianGioiHan,
                thoiGianHoanThanh = x.ThoiGianHoanThanh
            };
            await Clients.All.SendAsync("Receivedxuat", dto, id);
        }

        // Lấy chi tiết hàng hóa theo cửa — giữ nguyên nhưng đảm bảo null-safe
        public async Task Sendxexuat(int id)
        {
            var x = xrepo.GetXuat(id);
            if (x == null)
            {
                await Clients.All.SendAsync("Receivedxexuat", null, id);
                return;
            }
            var xexuat = xerepo.GetChitietXuat(x.Id);
            await Clients.All.SendAsync("Receivedxexuat", xexuat, id);
        }
        public async Task SenCuaXuatSL()
        {
            var sl = cuaXuatRepo.CuaXuatSL();
            await Clients.All.SendAsync("ReceivedCuaXuatSL", sl);
        }

        // Tổng hợp xuất — 3 cột: Chờ xuất / Đang xuất / Đã rời kho

        public async Task SendTongHopXuatFull()
        {
            try
            {
                var today = DateTime.Today;

                var tatCa = await dbContext.Xuats
                    .Where(x => x.ThoiGianPhanCong.Date == today)
                    .Include(x => x.ChitietXuats)
                    .Include(x => x.Xe)
                    .ToListAsync();

                // Phân nhóm theo trạng thái
                var choXuat = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DaPhanCong).ToList();
                var dangXuat = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DangBanGiao
                                               || x.TrangThai == (int)TrangThaiXuat.QuaThoiGian
                                               || x.TrangThai == (int)TrangThaiXuat.HoanThanh).ToList();
                var daRoiKho = tatCa.Where(x => x.TrangThai == (int)TrangThaiXuat.DaXuatPhat).ToList();

                // Tính tổng hàng hóa theo DonVi
                var hangHoaMap = new Dictionary<string, (long ChoXuat, long DangXuat, long DaRoi)>();

                void AddHang(List<Xuat> list, string cot)
                {
                    foreach (var x in list)
                    {
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
                }

                AddHang(choXuat, "cho");
                AddHang(dangXuat, "dang");
                AddHang(daRoiKho, "da");

                // Danh sách chi tiết từng xe để hiển thị bảng bên dưới
                var danhSach = tatCa
                    .Where(x => x.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                    .Select(x => new
                    {
                        XuatId = x.Id,
                        CuaXuatId = x.CuaXuatId,
                        BienSoXe = x.Xe?.BienSoXe ?? "--",
                        TrangThai = x.TrangThai,
                        ThoiGianVaoCua = x.ThoiGianVaoCua,
                        ThoiGianGioiHan = x.ThoiGianGioiHan,
                        HangHoa = x.ChitietXuats?.Select(c => new
                        {
                            c.DonVi,
                            c.ChuaBG,
                            c.DaBG
                        }).ToList()
                    }).ToList();

                var result = new
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
                    }).ToList(),
                    DanhSach = danhSach
                };

                await Clients.All.SendAsync("UpdateBangTongHopXuat", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi SendTongHopXuatFull: " + ex.Message);
            }
        }

        // ==================== NHẬP ====================

        public async Task SendNhap(int? id)
        {
            var x = nhapRepo.GetNhap(id);
            await Clients.All.SendAsync("ReceivedNhap", x, id);
        }

        public async Task SendChitietNhap(int id)
        {
            var x = nhapRepo.GetNhap(id);
            if (x == null)
            {
                await Clients.All.SendAsync("ReceivedChitietNhap", null, id);
            }
            else
            {
                var ct = chitietnhapRepo.GetChiTietNhap(x.Id);
                await Clients.All.SendAsync("ReceivedChitietNhap", ct, id);
            }
        }

        public async Task SenCuaNhapSL()
        {
            var sl = cuaNhapRepo.CuaNhapSL();
            await Clients.All.SendAsync("ReceivedCuaNhapSL", sl);
        }

        public async Task SendTongHopNhapFull()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ViettelApi");
                var response = await client.GetAsync("api/DanhSachXes/dashboard");
                if (!response.IsSuccessStatusCode)
                {
                    await Clients.All.SendAsync("UpdateBangTongHop", null);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var xes = JsonConvert.DeserializeObject<List<DanhSachXeDto>>(json) ?? new List<DanhSachXeDto>();

                int sapVeXe = 0, daVeXe = 0;
                var hangHoaMap = new Dictionary<string, (long SapVe, long DaVe)>();

                foreach (var xe in xes)
                {
                    var cx = xe.ChuyenHienTai;
                    if (cx == null) continue;

                    bool laSapVe = cx.TrangThai == 1;
                    bool laDaVe = cx.TrangThai == 2;

                    if (laSapVe) sapVeXe++;
                    if (laDaVe) daVeXe++;

                    foreach (var h in cx.HangHoas)
                    {
                        if (!hangHoaMap.ContainsKey(h.DonVi))
                            hangHoaMap[h.DonVi] = (0, 0);
                        var cur = hangHoaMap[h.DonVi];
                        hangHoaMap[h.DonVi] = (
                            SapVe: cur.SapVe + (laSapVe ? h.SoLuong : 0),
                            DaVe: cur.DaVe + (laDaVe ? h.SoLuong : 0)
                        );
                    }
                }

                var bienSoChuaVao = await dbContext.Nhaps
                    .Where(n => n.TrangThai == (int)TrangThaiNhap.DaPhanCong)
                    .Select(n => n.BienSoXe)
                    .ToListAsync();

                int chuaVaoXe = bienSoChuaVao.Count;
                var chuaVaoHangMap = new Dictionary<string, long>();

                foreach (var xe in xes)
                {
                    if (!bienSoChuaVao.Contains(xe.BienSo)) continue;
                    var cx = xe.ChuyenHienTai;
                    if (cx == null) continue;
                    foreach (var h in cx.HangHoas)
                    {
                        if (!chuaVaoHangMap.ContainsKey(h.DonVi))
                            chuaVaoHangMap[h.DonVi] = 0;
                        chuaVaoHangMap[h.DonVi] += h.SoLuong;
                    }
                }

                var result = new
                {
                    SapVeXe = sapVeXe,
                    DaVeXe = daVeXe,
                    ChuaVaoXe = chuaVaoXe,
                    HangHoas = hangHoaMap.Select(kv => new
                    {
                        DonVi = kv.Key,
                        SapVe = kv.Value.SapVe,
                        DaVe = kv.Value.DaVe,
                        ChuaVao = chuaVaoHangMap.GetValueOrDefault(kv.Key, 0L)
                    }).ToList()
                };

                await Clients.All.SendAsync("UpdateBangTongHop", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi SendTongHopNhapFull: " + ex.Message);
            }
        }
        public async Task SendTrangThaiXe(int xeId, string trangThai)
        {
            await Clients.All.SendAsync("TrangThaiXeUpdated", xeId, trangThai);
        }
        // Thêm vào cuối class SignalServer
        public async Task ReloadDashboard()
        {
            await Clients.All.SendAsync("ReloadDashboard");
        }
    }
}
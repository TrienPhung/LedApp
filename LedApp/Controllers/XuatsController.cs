using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class XuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;

        public XuatsController(ApplicationDBContext context, IHubContext<SignalServer> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Xuats
        public async Task<IActionResult> Index()
        {
            var applicationDBContext = _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Include(x => x.NhanVienXacNhan);
            return View(await applicationDBContext.ToListAsync());
        }

        // GET: Xuats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var xuat = await _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Include(x => x.NhanVienXacNhan)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (xuat == null) return NotFound();
            return View(xuat);
        }

        // GET: Xuats/Create
        public IActionResult Create()
        {
            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten");
            ViewData["XeId"] = new SelectList(
                _context.DanhSachXes.Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai),
                "Id", "BienSoXe");
            ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name");
            return View();
        }

        // POST: Xuats/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("Id,CuaXuatId,XeId,ThoiGianPhanCong,NhanVienXacNhanId,GhiChu")] Xuat xuat)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                xuat.TrangThai = (int)TrangThaiXuat.DaPhanCong;
                _context.Add(xuat);
                await _context.SaveChangesAsync();

                if (xuat.XeId.HasValue)
                {
                    var xe = await _context.DanhSachXes.FindAsync(xuat.XeId.Value);
                    if (xe != null)
                    {
                        xe.TrangThai = (int)TrangThaiXe.DangPhanCong;
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                await PushTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
                ViewData["XeId"] = new SelectList(
                    _context.DanhSachXes.Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai),
                    "Id", "BienSoXe", xuat.XeId);
                ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name", xuat.NhanVienXacNhanId);
                return View(xuat);
            }
        }

        // GET: Xuats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var xuat = await _context.Xuats.FindAsync(id);
            if (xuat == null) return NotFound();

            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
            ViewData["XeId"] = new SelectList(_context.DanhSachXes, "Id", "BienSoXe", xuat.XeId);
            ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name", xuat.NhanVienXacNhanId);
            return View(xuat);
        }

        // POST: Xuats/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,CuaXuatId,XeId,ThoiGianPhanCong,ThoiGianVaoCua,ThoiGianGioiHan,ThoiGianHoanThanh,ThoiGianXuatPhat,NhanVienXacNhanId,TrangThai,GhiChu")] Xuat xuat)
        {
            if (id != xuat.Id) return NotFound();
            try
            {
                _context.Update(xuat);
                await _context.SaveChangesAsync();
                await PushTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
                ViewData["XeId"] = new SelectList(_context.DanhSachXes, "Id", "BienSoXe", xuat.XeId);
                ViewData["NhanVienXacNhanId"] = new SelectList(_context.nguoiDungs, "Id", "Name", xuat.NhanVienXacNhanId);
                return View(xuat);
            }
        }

        // GET: Xuats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var xuat = await _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Include(x => x.NhanVienXacNhan)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (xuat == null) return NotFound();
            return View(xuat);
        }

        // POST: Xuats/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats.FindAsync(id);
                if (xuat != null)
                {
                    // ← LẤY XeId TRƯỚC KHI XÓA
                    var xeId = xuat.XeId;
                    var trangThaiXuat = xuat.TrangThai;
                    // Xóa ChitietXuat trước
                    var chitiets = _context.ChitietXuats.Where(c => c.XuatId == id);
                    _context.ChitietXuats.RemoveRange(chitiets);

                    _context.Xuats.Remove(xuat);
                    await _context.SaveChangesAsync();

                    // ← CẬP NHẬT XE SAU KHI XÓA XONG
                    if (xeId.HasValue && trangThaiXuat != (int)TrangThaiXuat.DaXuatPhat)
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
                }
                await transaction.CommitAsync();
                await PushTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper: build và push tổng hợp xuất qua IHubContext
        private async Task PushTongHopXuat()
        {
            var today = DateTime.Today;
            var tatCa = await _context.Xuats
                .Where(x => x.ThoiGianPhanCong.Date == today)
                .Include(x => x.ChitietXuats)
                .Include(x => x.Xe)
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
                }).ToList()
            };

            await _hubContext.Clients.All.SendAsync("UpdateBangTongHopXuat", result);
        }

        private bool XuatExists(int id)
        {
            return (_context.Xuats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
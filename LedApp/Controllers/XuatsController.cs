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
using Microsoft.AspNetCore.Identity;

namespace LedApp.Controllers
{
    [Authorize]
    public class XuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly UserManager<AppUser> _userManager;

        public XuatsController(ApplicationDBContext context, IHubContext<SignalServer> hubContext, UserManager<AppUser> userManager)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        private async Task<List<nguoiDungs>> GetDanhSachQuanLy()
        {
            var userIds = (await _userManager.GetUsersInRoleAsync("QuanLy"))
                .Select(u => u.Id).ToHashSet();

            return _context.nguoiDungs
                .AsEnumerable()
                .Where(nv => nv.UserId != null && userIds.Contains(nv.UserId))
                .ToList();
        }

        // GET: Xuats
        // Index - xem danh sách
        [Authorize(Policy = "Xuat.View")]
        public async Task<IActionResult> Index()
        {
            var applicationDBContext = _context.Xuats
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Include(x => x.NhanVienXacNhan);
            return View(await applicationDBContext.ToListAsync());
        }

        // GET: Xuats/Details/5
        // Details - xem chi tiết
        [Authorize(Policy = "Xuat.View")]
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
        // Create GET
        [Authorize(Policy = "Xuat.Create")]
        public async Task<IActionResult> Create()
        {
            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten");
            ViewData["XeId"] = new SelectList(
                await _context.DanhSachXes
                    .AsNoTracking()
                    .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai)
                    .ToListAsync(),
                "Id", "BienSoXe");
            ViewData["NhanVienXacNhanId"] = new SelectList(
                await GetDanhSachQuanLy(), "Id", "FullName");
            return View();
        }

        // POST: Xuats/Create
        [HttpPost]
        [Authorize(Policy = "Xuat.Create")]
        public async Task<IActionResult> Create(
            [Bind("Id,CuaXuatId,XeId,ThoiGianPhanCong,ThoiGianGioiHan,DiaDiemGiao,NhanVienXacNhanId,GhiChu")] Xuat xuat)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                xuat.TrangThai = (int)TrangThaiXuat.DaPhanCong;
                _context.Add(xuat);
                await _context.SaveChangesAsync();

                if (xuat.XeId.HasValue)
                {
                    var xe = await _context.DanhSachXes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == xuat.XeId.Value);
                    if (xe != null)
                    {
                        xe.TrangThai = (int)TrangThaiXe.DangPhanCong;
                        _context.Entry(xe).State = EntityState.Modified;
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
                TempData["Error"] = ex.Message;
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
                ViewData["XeId"] = new SelectList(
                    _context.DanhSachXes.Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai),
                    "Id", "BienSoXe", xuat.XeId);
                ViewData["NhanVienXacNhanId"] = new SelectList(
                    await GetDanhSachQuanLy(), "Id", "FullName", xuat.NhanVienXacNhanId);
                return View(xuat);
            }
        }

        // GET: Xuats/Edit/5
        [Authorize(Policy = "Xuat.Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var xuat = await _context.Xuats.FindAsync(id);
            if (xuat == null) return NotFound();

            var danhSachXe = _context.DanhSachXes.Where(x =>
                x.TrangThai == (int)TrangThaiXe.TrongBai || x.Id == xuat.XeId);

            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
            ViewData["XeId"] = new SelectList(danhSachXe, "Id", "BienSoXe", xuat.XeId);
            ViewData["NhanVienXacNhanId"] = new SelectList(
                await GetDanhSachQuanLy(), "Id", "FullName", xuat.NhanVienXacNhanId);
            return View(xuat);
        }

        // POST: Xuats/Edit/5
        [HttpPost]
        [Authorize(Policy = "Xuat.Edit")]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,CuaXuatId,XeId,ThoiGianPhanCong,ThoiGianVaoCua,ThoiGianGioiHan," +
          "ThoiGianHoanThanh,ThoiGianXuatPhat,ThoiGianDuKienVeBai,ThoiGianVeBai," +
          "DiaDiemGiao,NhanVienXacNhanId,TrangThai,GhiChu")] Xuat xuat)
        {
            if (id != xuat.Id) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuatCu = await _context.Xuats.AsNoTracking()
                                 .FirstOrDefaultAsync(x => x.Id == id);

                if (xuatCu?.XeId != null && xuatCu.XeId != xuat.XeId)
                {
                    var xeCu = await _context.DanhSachXes.AsTracking()
                                   .FirstOrDefaultAsync(x => x.Id == xuatCu.XeId.Value);
                    if (xeCu != null)
                    {
                        xeCu.TrangThai = (int)TrangThaiXe.TrongBai;
                        await _context.SaveChangesAsync();
                    }
                }

                if (xuat.XeId.HasValue && xuat.XeId != xuatCu?.XeId)
                {
                    var xeMoi = await _context.DanhSachXes.AsTracking()
                                    .FirstOrDefaultAsync(x => x.Id == xuat.XeId.Value);
                    if (xeMoi != null)
                    {
                        xeMoi.TrangThai = (int)TrangThaiXe.DangPhanCong;
                        await _context.SaveChangesAsync();
                    }
                }

                _context.Update(xuat);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                await PushTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Ten", xuat.CuaXuatId);
                ViewData["XeId"] = new SelectList(_context.DanhSachXes, "Id", "BienSoXe", xuat.XeId);
                ViewData["NhanVienXacNhanId"] = new SelectList(
                    await GetDanhSachQuanLy(), "Id", "FullName", xuat.NhanVienXacNhanId);
                return View(xuat);
            }
        }

        // GET: Xuats/Delete/5
        [Authorize(Policy = "Xuat.Delete")]
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
        [Authorize(Policy = "Xuat.Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var xuat = await _context.Xuats.FindAsync(id);
                if (xuat != null)
                {
                    var xeId = xuat.XeId;
                    var trangThaiXuat = xuat.TrangThai;

                    var chitiets = _context.ChitietXuats.Where(c => c.XuatId == id);
                    _context.ChitietXuats.RemoveRange(chitiets);
                    _context.Xuats.Remove(xuat);
                    await _context.SaveChangesAsync();

                    if (xeId.HasValue && trangThaiXuat != (int)TrangThaiXuat.DaXuatPhat)
                    {
                        var conPhieuKhac = await _context.Xuats
                            .AnyAsync(x => x.XeId == xeId.Value && x.Id != id);

                        if (!conPhieuKhac)
                        {
                            var xe = await _context.DanhSachXes
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == xeId.Value);
                            if (xe != null)
                            {
                                xe.TrangThai = (int)TrangThaiXe.TrongBai;
                                _context.Entry(xe).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
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

        // POST: Xuats/BulkDelete
        [HttpPost]
        [Authorize(Policy = "Xuat.Delete")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var id in request.Ids)
                {
                    var xuat = await _context.Xuats.FindAsync(id);
                    if (xuat == null) continue;

                    var xeId = xuat.XeId;
                    var trangThaiXuat = xuat.TrangThai;

                    // Xóa ChitietXuat trước
                    var chitiets = _context.ChitietXuats.Where(c => c.XuatId == id);
                    _context.ChitietXuats.RemoveRange(chitiets);
                    _context.Xuats.Remove(xuat);
                    await _context.SaveChangesAsync();

                    // Trả xe về TrongBai nếu chưa xuất phát và không còn phiếu khác
                    if (xeId.HasValue && trangThaiXuat != (int)TrangThaiXuat.DaXuatPhat)
                    {
                        var conPhieuKhac = await _context.Xuats
                            .AnyAsync(x => x.XeId == xeId.Value);

                        if (!conPhieuKhac)
                        {
                            var xe = await _context.DanhSachXes
                                .FirstOrDefaultAsync(x => x.Id == xeId.Value);
                            if (xe != null)
                            {
                                xe.TrangThai = (int)TrangThaiXe.TrongBai;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
                await PushTongHopXuat();
                return Ok(new { message = $"Đã xóa {request.Ids.Count} phiếu xuất." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Helper: push tổng hợp xuất qua SignalR
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

    // Request model cho BulkDelete
    public class BulkDeleteRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
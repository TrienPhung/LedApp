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
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class XuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly SignalServer signalServer;

        public XuatsController(ApplicationDBContext context, SignalServer signalServer)
        {
            _context = context;
            this.signalServer = signalServer;
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
            // Chỉ hiện xe đang rảnh (TrongBai)
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
                // Mặc định trạng thái khi tạo mới
                xuat.TrangThai = (int)TrangThaiXuat.DaPhanCong;

                _context.Add(xuat);
                await _context.SaveChangesAsync();

                // Đồng thời đổi trạng thái xe sang DangPhanCong
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
                await signalServer.SendTongHopXuatFull();
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
                await signalServer.SendTongHopXuatFull();
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
                    // Trả xe về TrongBai nếu chưa DaXuatPhat
                    if (xuat.XeId.HasValue && xuat.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                    {
                        var xe = await _context.DanhSachXes.FindAsync(xuat.XeId.Value);
                        if (xe != null)
                        {
                            xe.TrangThai = (int)TrangThaiXe.TrongBai;
                        }
                    }
                    _context.Xuats.Remove(xuat);
                    await _context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
                await signalServer.SendTongHopXuatFull();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        private bool XuatExists(int id)
        {
            return (_context.Xuats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
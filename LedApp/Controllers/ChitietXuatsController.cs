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
    public class ChitietXuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        SignalServer signalServer;

        public ChitietXuatsController(ApplicationDBContext context, SignalServer signalServer)
        {
            //this._context = context ?? throw new ArgumentNullException("MyCoolDbContext is null", (Exception)null);
            this._context = context;
            this.signalServer = signalServer;
        }

        // GET: ChitietXuats
        public async Task<IActionResult> Index()
        {
            var data = await _context.Xuats
                .AsNoTracking()
                .Include(x => x.CuaXuat)
                .Include(x => x.Xe)
                .Include(x => x.ChitietXuats)
                .OrderBy(x => x.Id)
                .ToListAsync();
            return View(data);
        }
        // GET: ChitietXuats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ChitietXuats == null)
            {
                return NotFound();
            }

            var chitietXuat = await _context.ChitietXuats
                .Include(c => c.Xuat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chitietXuat == null)
            {
                return NotFound();
            }

            return View(chitietXuat);
        }

        // GET: ChitietXuats/Create
        public IActionResult Create()
        {
            var xuatList = _context.Xuats
                .Include(x => x.Xe)
                .ToList()
                .Select(s => new {
                    Id = s.Id,
                    Ten = s.Id + " - " + (s.Xe != null ? s.Xe.BienSoXe : "Chưa có xe")
                });

            ViewData["XuatId"] = new SelectList(xuatList, "Id", "Ten");
            return View();
        }

        // POST: ChitietXuats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,XuatId,DonVi,ChuaBG,DaBG")] ChitietXuat chitietXuat)
        {
            try
            {
                _context.Add(chitietXuat);
                await _context.SaveChangesAsync();
                _context.Entry(chitietXuat).State = EntityState.Detached;
                //signalServer.Sendxexuat();
                //signalServer.Sendxexuat2();
                //signalServer.Sendxexuat3();
                //signalServer.Sendxexuat4();
                //signalServer.Sendxexuat5();
                //signalServer.Sendxexuat6();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["XuatId"] = new SelectList(
                    _context.Xuats.Where(s => s.ThoiGianPhanCong.Date == DateTime.Today),
                    "Id", "Id", chitietXuat.XuatId);
                return View(chitietXuat);
            }
        }

        // GET: ChitietXuats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ChitietXuats == null)
            {
                return NotFound();
            }

            var chitietXuat = await _context.ChitietXuats.FindAsync(id);
            if (chitietXuat == null)
            {
                return NotFound();
            }
            ViewData["XuatId"] = new SelectList(_context.Xuats, "Id", "Id", chitietXuat.XuatId);
            return View(chitietXuat);
        }

        // POST: ChitietXuats/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,XuatId,DonVi,ChuaBG,DaBG")] ChitietXuat chitietXuat)
        {
            if (id != chitietXuat.Id)
            {
                return NotFound();
            }
            try
            {
                _context.Update(chitietXuat);
                await _context.SaveChangesAsync();
                _context.Entry(chitietXuat).State = EntityState.Detached;
                //signalServer.Sendxexuat();
                //signalServer.Sendxexuat2();
                //signalServer.Sendxexuat3();
                //signalServer.Sendxexuat4();
                //signalServer.Sendxexuat5();
                //signalServer.Sendxexuat6();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ViewData["XuatId"] = new SelectList(_context.Xuats, "Id", "Id", chitietXuat.XuatId);
                return View(chitietXuat);
            }
        }

        // GET: ChitietXuats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ChitietXuats == null)
            {
                return NotFound();
            }

            var chitietXuat = await _context.ChitietXuats
                .Include(c => c.Xuat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chitietXuat == null)
            {
                return NotFound();
            }

            return View(chitietXuat);
        }

        // POST: ChitietXuats/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ChitietXuats == null)
            {
                return Problem("Entity set 'ApplicationDBContext.ChitietXuats'  is null.");
            }
            var chitietXuat = await _context.ChitietXuats.FindAsync(id);
            if (chitietXuat != null)
            {
                _context.ChitietXuats.Remove(chitietXuat);
                await _context.SaveChangesAsync(); // ✅ trong if, tránh null reference
            }
            //signalServer.Sendxexuat();
            //signalServer.Sendxexuat2();
            //signalServer.Sendxexuat3();
            //signalServer.Sendxexuat4();
            //signalServer.Sendxexuat5();
            //signalServer.Sendxexuat6();
            return RedirectToAction(nameof(Index));
        }

        private bool ChitietXuatExists(int id)
        {
            return (_context.ChitietXuats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
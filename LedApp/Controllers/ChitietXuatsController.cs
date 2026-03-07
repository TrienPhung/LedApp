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

namespace LedApp.Controllers
{
    [Authorize]
    public class ChitietXuatsController : Controller
    {
        ApplicationDBContext _context;
        SignalServer signalServer;
        public ChitietXuatsController(ApplicationDBContext context, SignalServer signalServer)
        {
            this._context = context;
            //this._context = context ?? throw new ArgumentNullException("MyCoolDbContext is null", (Exception)null);
            this.signalServer = signalServer;
        }

        // GET: ChitietXuats
        public async Task<IActionResult> Index()
        {
              return _context.ChitietXuats != null ? 
                          View(await _context.ChitietXuats.OrderBy(s=>s.dulieuxuatId).ToListAsync()) :
                          Problem("Entity set 'ApplicationDBContext.ChitietXuats'  is null.");
        }

        // GET: ChitietXuats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ChitietXuats == null)
            {
                return NotFound();
            }

            var chitietXuat = await _context.ChitietXuats
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
            ViewData["dulieuxuatId"] = new SelectList(_context.dulieuxuats.Where(s=>s.NgayXuat==DateTime.Today), "Id","BienSoXe");
            return View();
        }

        // POST: ChitietXuats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SoluongCH,donviCH,SoluongD,donviD,dulieuxuatId")] ChitietXuat chitietXuat)
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
            catch(Exception ex)
            {
                ViewData["dulieuxuatId"] = new SelectList(_context.dulieuxuats.Where(s => s.NgayXuat == DateTime.Today), "Id", "BienSoXe");
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
            ViewData["dulieuxuatId"] = new SelectList(_context.dulieuxuats, "Id", "Id");
            return View(chitietXuat);
        }

        // POST: ChitietXuats/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SoluongCH,donviCH,SoluongD,donviD,dulieuxuatId")] ChitietXuat chitietXuat)
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
                ViewData["dulieuxuatId"] = new SelectList(_context.dulieuxuats, "Id", "Id");
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chitietXuat == null)
            {
                return NotFound();
            }
            ViewData["dulieuxuatId"] = new SelectList(_context.dulieuxuats, "Id", "Id");
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
            }
            
            await _context.SaveChangesAsync();
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

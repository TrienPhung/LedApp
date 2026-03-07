using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;

namespace LedApp.Controllers
{
    [Authorize]
    public class ChitietNhapsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public ChitietNhapsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: ChitietNhaps
        public async Task<IActionResult> Index()
        {
              return _context.ChitietNhaps != null ? 
                          View(await _context.ChitietNhaps.ToListAsync()) :
                          Problem("Entity set 'ApplicationDBContext.ChitietNhaps'  is null.");
        }

        // GET: ChitietNhaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.ChitietNhaps == null)
            {
                return NotFound();
            }

            var chitietNhap = await _context.ChitietNhaps
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chitietNhap == null)
            {
                return NotFound();
            }

            return View(chitietNhap);
        }

        // GET: ChitietNhaps/Create
        public IActionResult Create()
        {
            ViewData["NhapId"] = new SelectList(_context.Nhaps, "Id", "Id");
            return View();
        }

        // POST: ChitietNhaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NhapId,Soluong,donvi")] ChitietNhap chitietNhap)
        {
           try
            {
                _context.Add(chitietNhap);
                await _context.SaveChangesAsync();
                _context.Entry(chitietNhap).State = EntityState.Detached;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["NhapId"] = new SelectList(_context.Nhaps, "Id", "Id");
                return View(chitietNhap);
            }
           
        }

        // GET: ChitietNhaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.ChitietNhaps == null)
            {
                return NotFound();
            }

            var chitietNhap = await _context.ChitietNhaps.FindAsync(id);
            if (chitietNhap == null)
            {
                return NotFound();
            }
            ViewData["NhapId"] = new SelectList(_context.Nhaps, "Id", "Id");
            return View(chitietNhap);
        }

        // POST: ChitietNhaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NhapId,Soluong,donvi")] ChitietNhap chitietNhap)
        {
            if (id != chitietNhap.Id)
            {
                return NotFound();
            }
            try
            {
                _context.Update(chitietNhap);
                await _context.SaveChangesAsync();
                _context.Entry(chitietNhap).State = EntityState.Detached;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ViewData["NhapId"] = new SelectList(_context.Nhaps, "Id", "Id");
                return View(chitietNhap);
            }
        }

        // GET: ChitietNhaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.ChitietNhaps == null)
            {
                return NotFound();
            }

            var chitietNhap = await _context.ChitietNhaps
                .FirstOrDefaultAsync(m => m.Id == id);
            if (chitietNhap == null)
            {
                return NotFound();
            }

            return View(chitietNhap);
        }

        // POST: ChitietNhaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.ChitietNhaps == null)
            {
                return Problem("Entity set 'ApplicationDBContext.ChitietNhaps'  is null.");
            }
            var chitietNhap = await _context.ChitietNhaps.FindAsync(id);
            if (chitietNhap != null)
            {
                _context.ChitietNhaps.Remove(chitietNhap);
            }
            
            await _context.SaveChangesAsync();
            _context.Entry(chitietNhap).State = EntityState.Detached;
            return RedirectToAction(nameof(Index));
        }

        private bool ChitietNhapExists(int id)
        {
          return (_context.ChitietNhaps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

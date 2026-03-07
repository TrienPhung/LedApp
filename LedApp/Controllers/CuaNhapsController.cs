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
    public class CuaNhapsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public CuaNhapsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: CuaNhaps
        public async Task<IActionResult> Index()
        {
              return _context.CuaNhaps != null ? 
                          View(await _context.CuaNhaps.ToListAsync()) :
                          Problem("Entity set 'ApplicationDBContext.CuaNhaps'  is null.");
        }

        // GET: CuaNhaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaNhaps == null)
            {
                return NotFound();
            }

            var cuaNhap = await _context.CuaNhaps
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cuaNhap == null)
            {
                return NotFound();
            }

            return View(cuaNhap);
        }

        // GET: CuaNhaps/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CuaNhaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ten,Mota")] CuaNhap cuaNhap)
        {
            //if (ModelState.IsValid)
            try{
                _context.Add(cuaNhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }catch(Exception ex)
            {
                return View(cuaNhap);
            }
            //
        }

        // GET: CuaNhaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaNhaps == null)
            {
                return NotFound();
            }

            var cuaNhap = await _context.CuaNhaps.FindAsync(id);
            if (cuaNhap == null)
            {
                return NotFound();
            }
            return View(cuaNhap);
        }

        // POST: CuaNhaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,Mota")] CuaNhap cuaNhap)
        {
            if (id != cuaNhap.Id)
            {
                return NotFound();
            }

            try
            {
                _context.Update(cuaNhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                return View(cuaNhap);
            }
            
        }

        // GET: CuaNhaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaNhaps == null)
            {
                return NotFound();
            }

            var cuaNhap = await _context.CuaNhaps
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cuaNhap == null)
            {
                return NotFound();
            }

            return View(cuaNhap);
        }

        // POST: CuaNhaps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CuaNhaps == null)
            {
                return Problem("Entity set 'ApplicationDBContext.CuaNhaps'  is null.");
            }
            var cuaNhap = await _context.CuaNhaps.FindAsync(id);
            if (cuaNhap != null)
            {
                _context.CuaNhaps.Remove(cuaNhap);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CuaNhapExists(int id)
        {
          return (_context.CuaNhaps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

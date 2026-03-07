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
    public class NhapsController : Controller
    {
        private readonly ApplicationDBContext _context;

        public NhapsController(ApplicationDBContext context)
        {
            _context = context;
        }

        // GET: Nhaps
        public async Task<IActionResult> Index()
        {
            var applicationDBContext = _context.Nhaps.Include(n => n.CuaNhap);
            return View(await applicationDBContext.ToListAsync());
        }

        // GET: Nhaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Nhaps == null)
            {
                return NotFound();
            }

            var nhap = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nhap == null)
            {
                return NotFound();
            }

            return View(nhap);
        }

        // GET: Nhaps/Create
        public IActionResult Create()
        {
            ViewData["CuaNhapId"] = new SelectList(_context.CuaNhaps, "Id", "Id");
            return View();
        }

        // POST: Nhaps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CuaNhapId,BienSoXe,NgayNhap,GioNhap,PhutNhap,CongVao,TrangThai")] Nhap nhap)
        {
            try 
            {
                _context.Add(nhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                ViewData["CuaNhapId"] = new SelectList(_context.CuaNhaps, "Id", "Id", nhap.CuaNhapId);
                return View(nhap);
            }
            
        }

        // GET: Nhaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Nhaps == null)
            {
                return NotFound();
            }

            var nhap = await _context.Nhaps.FindAsync(id);
            if (nhap == null)
            {
                return NotFound();
            }
            ViewData["CuaNhapId"] = new SelectList(_context.CuaNhaps, "Id", "Id", nhap.CuaNhapId);
            return View(nhap);
        }

        // POST: Nhaps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CuaNhapId,BienSoXe,NgayNhap,GioNhap,PhutNhap,CongVao,TrangThai")] Nhap nhap)
        {
            if (id != nhap.Id)
            {
                return NotFound();
            }
            try
            {
                _context.Update(nhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ViewData["CuaNhapId"] = new SelectList(_context.CuaNhaps, "Id", "Id", nhap.CuaNhapId);
                return View(nhap);
            }
        }

        // GET: Nhaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Nhaps == null)
            {
                return NotFound();
            }

            var nhap = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (nhap == null)
            {
                return NotFound();
            }

            return View(nhap);
        }

        // POST: Nhaps/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Nhaps == null)
            {
                return Problem("Entity set 'ApplicationDBContext.Nhaps'  is null.");
            }
            var nhap = await _context.Nhaps.FindAsync(id);
            if (nhap != null)
            {
                _context.Nhaps.Remove(nhap);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NhapExists(int id)
        {
          return (_context.Nhaps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

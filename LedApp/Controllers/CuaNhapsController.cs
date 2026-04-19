using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CuaNhapsController : Controller
    {
        private readonly ApplicationDBContext _context;
        public CuaNhapsController(ApplicationDBContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            _context.CuaNhaps != null
                ? View(await _context.CuaNhaps.ToListAsync())
                : Problem("Entity set 'ApplicationDBContext.CuaNhaps' is null.");

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Ten,Mota,IsActive")] CuaNhap cuaNhap)
        {
            try
            {
                _context.Add(cuaNhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch { return View(cuaNhap); }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FindAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,Mota,IsActive")] CuaNhap cuaNhap)
        {
            if (id != cuaNhap.Id) return NotFound();
            try
            {
                var existing = await _context.CuaNhaps.AsTracking()
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (existing == null) return NotFound();

                existing.Ten = cuaNhap.Ten;
                existing.Mota = cuaNhap.Mota;
                existing.IsActive = cuaNhap.IsActive;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException) { return View(cuaNhap); }
        }

        // Toggle nhanh không cần vào form Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var item = await _context.CuaNhaps.FindAsync(id);
            if (item == null) return NotFound();
            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaNhaps == null) return NotFound();
            var item = await _context.CuaNhaps.FirstOrDefaultAsync(m => m.Id == id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CuaNhaps == null)
                return Problem("Entity set 'ApplicationDBContext.CuaNhaps' is null.");
            var item = await _context.CuaNhaps.FindAsync(id);
            if (item != null) _context.CuaNhaps.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool CuaNhapExists(int id) =>
             (_context.CuaNhaps?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}

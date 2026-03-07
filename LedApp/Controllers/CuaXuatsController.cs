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
    public class CuaXuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        SignalServer signalServer;
        public CuaXuatsController(ApplicationDBContext context, SignalServer signalServer)
        {
            _context = context;
            this.signalServer = signalServer;
        }

        // GET: CuaXuats
        public async Task<IActionResult> Index()
        {
              return _context.CuaXuats != null ? 
                          View(await _context.CuaXuats.ToListAsync()) :
                          Problem("Entity set 'ApplicationDBContext.CuaXuats'  is null.");
        }

        // GET: CuaXuats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.CuaXuats == null)
            {
                return NotFound();
            }

            var cuaXuat = await _context.CuaXuats
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cuaXuat == null)
            {
                return NotFound();
            }

            return View(cuaXuat);
        }

        // GET: CuaXuats/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CuaXuats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ten,Mota")] CuaXuat cuaXuat)
        {
            try
            { 
                _context.Add(cuaXuat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
             catch (Exception ex)
            {
                return View(cuaXuat);
            }
           
        }

        // GET: CuaXuats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.CuaXuats == null)
            {
                return NotFound();
            }

            var cuaXuat = await _context.CuaXuats.FindAsync(id);
            if (cuaXuat == null)
            {
                return NotFound();
            }
            return View(cuaXuat);
        }

        // POST: CuaXuats/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,Mota")] CuaXuat cuaXuat)
        {
            if (id != cuaXuat.Id)
            {
                return NotFound();
            }
            try
            {
                _context.Update(cuaXuat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return View(cuaXuat);
            }

        }

        // GET: CuaXuats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.CuaXuats == null)
            {
                return NotFound();
            }

            var cuaXuat = await _context.CuaXuats
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cuaXuat == null)
            {
                return NotFound();
            }

            return View(cuaXuat);
        }

        // POST: CuaXuats/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.CuaXuats == null)
            {
                return Problem("Entity set 'ApplicationDBContext.CuaXuats'  is null.");
            }
            var cuaXuat = await _context.CuaXuats.FindAsync(id);
            if (cuaXuat != null)
            {
                _context.CuaXuats.Remove(cuaXuat);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CuaXuatExists(int id)
        {
          return (_context.CuaXuats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

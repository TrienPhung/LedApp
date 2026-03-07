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
    public class dulieuxuatsController : Controller
    {
        private readonly ApplicationDBContext _context;
        SignalServer signalServer;
        public dulieuxuatsController(ApplicationDBContext context, SignalServer signalServer)
        {
            _context = context;
            this.signalServer = signalServer;
        }

        // GET: dulieuxuats
        public async Task<IActionResult> Index()
        {
            var applicationDBContext = _context.dulieuxuats.Include(d => d.CuaXuat);
            return View(await applicationDBContext.ToListAsync());
        }

        // GET: dulieuxuats/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.dulieuxuats == null)
            {
                return NotFound();
            }

            var dulieuxuat = await _context.dulieuxuats
                .Include(d => d.CuaXuat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dulieuxuat == null)
            {
                return NotFound();
            }

            return View(dulieuxuat);
        }

        // GET: dulieuxuats/Create
        public IActionResult Create()
        {
            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Id");
            return View();
        }

        // POST: dulieuxuats/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CuaXuatId,BienSoXe,NgayXuat,GioXuat,PhutXuat,CongRa,TrangThai")] dulieuxuat dulieuxuat)
        {
            try
            {
                _context.Add(dulieuxuat);
                await _context.SaveChangesAsync();
                _context.Entry(dulieuxuat).State = EntityState.Detached;
                //signalServer.Sendxuat();
                //signalServer.Sendxuat2();
                //signalServer.Sendxuat3();
                //signalServer.Sendxuat4();
                //signalServer.Sendxuat5();
                //signalServer.Sendxuat6();
              //  signalServer.SendTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) {
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Id", dulieuxuat.CuaXuatId);
                return View(dulieuxuat);
            }
           
        }

        // GET: dulieuxuats/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.dulieuxuats == null)
            {
                return NotFound();
            }

            var dulieuxuat = await _context.dulieuxuats.FindAsync(id);
            if (dulieuxuat == null)
            {
                return NotFound();
            }
            ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Id", dulieuxuat.CuaXuatId);
            return View(dulieuxuat);
        }

        // POST: dulieuxuats/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CuaXuatId,BienSoXe,NgayXuat,GioXuat,PhutXuat,CongRa,TrangThai")] dulieuxuat dulieuxuat)
        {
            if (id != dulieuxuat.Id)
            {
                return NotFound();
            }
            try
            {
                _context.Update(dulieuxuat);
                await _context.SaveChangesAsync();
                _context.Entry(dulieuxuat).State = EntityState.Detached;
                //signalServer.Sendxuat();
                //signalServer.Sendxuat2();
                //signalServer.Sendxuat3();
                //signalServer.Sendxuat4();
                //signalServer.Sendxuat5();
                //signalServer.Sendxuat6();
               // signalServer.SendTongHopXuat();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["CuaXuatId"] = new SelectList(_context.CuaXuats, "Id", "Id", dulieuxuat.CuaXuatId);
                return View(dulieuxuat);
            }

        }

        // GET: dulieuxuats/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.dulieuxuats == null)
            {
                return NotFound();
            }

            var dulieuxuat = await _context.dulieuxuats
                .Include(d => d.CuaXuat)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (dulieuxuat == null)
            {
                return NotFound();
            }

            return View(dulieuxuat);
        }

        // POST: dulieuxuats/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.dulieuxuats == null)
            {
                return Problem("Entity set 'ApplicationDBContext.dulieuxuats'  is null.");
            }
            var dulieuxuat = await _context.dulieuxuats.FindAsync(id);
            if (dulieuxuat != null)
            {
                _context.dulieuxuats.Remove(dulieuxuat);
            }
            
            await _context.SaveChangesAsync();
            _context.Entry(dulieuxuat).State = EntityState.Detached;
            //signalServer.Sendxuat();
            //signalServer.Sendxuat2();
            //signalServer.Sendxuat3();
            //signalServer.Sendxuat4();
            //signalServer.Sendxuat5();
            //signalServer.Sendxuat6();
          //  signalServer.SendTongHopXuat();
            return RedirectToAction(nameof(Index));
        }

        private bool dulieuxuatExists(int id)
        {
          return (_context.dulieuxuats?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

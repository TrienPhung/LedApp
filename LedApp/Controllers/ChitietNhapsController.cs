using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

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

        // Helper — dùng chung cho Create và Edit
        private SelectList GetNhapSelectList(int? selectedId = null)
        {
            var list = _context.Nhaps
                .Include(n => n.CuaNhap)
                .Select(n => new {
                    Id = n.Id,
                    Ten = n.Id + " - " + n.BienSoXe + " - " + n.CuaNhap.Ten
                })
                .ToList();
            return new SelectList(list, "Id", "Ten", selectedId);
        }

        // GET: ChitietNhaps
        public async Task<IActionResult> Index()
        {
            var data = await _context.Nhaps
                .Include(n => n.CuaNhap)
                .Include(n => n.ChitietNhaps)
                .OrderBy(n => n.Id)
                .ToListAsync();
            return View(data);
        }

        // GET: ChitietNhaps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var chitietNhap = await _context.ChitietNhaps
                .Include(c => c.Nhap)
                    .ThenInclude(n => n.CuaNhap)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (chitietNhap == null) return NotFound();
            return View(chitietNhap);
        }

        // GET: ChitietNhaps/Create
        public IActionResult Create()
        {
            ViewData["NhapId"] = GetNhapSelectList();
            return View();
        }

        // POST: ChitietNhaps/Create
        [HttpPost]
        public async Task<IActionResult> Create([Bind("NhapId,DonVi,ChuaBG,DaBG")] ChitietNhap chitietNhap)
        {
            try
            {
                _context.Add(chitietNhap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                ViewData["NhapId"] = GetNhapSelectList(chitietNhap.NhapId);
                return View(chitietNhap);
            }
        }

        // GET: ChitietNhaps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var chitietNhap = await _context.ChitietNhaps
                .Include(c => c.Nhap)
                    .ThenInclude(n => n.CuaNhap)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chitietNhap == null) return NotFound();
            return View(chitietNhap);
        }

        // POST: ChitietNhaps/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NhapId,DonVi,ChuaBG,DaBG")] ChitietNhap chitietNhap)
        {
            if (id != chitietNhap.Id) return NotFound();

            try
            {
                var existing = await _context.ChitietNhaps
                    .AsTracking() 
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (existing == null) return NotFound();

                existing.DonVi = chitietNhap.DonVi;
                existing.ChuaBG = chitietNhap.ChuaBG;
                existing.DaBG = chitietNhap.DaBG;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChitietNhapExists(chitietNhap.Id)) return NotFound();

                var chitiet = await _context.ChitietNhaps
                    .Include(c => c.Nhap)
                        .ThenInclude(n => n.CuaNhap)
                    .FirstOrDefaultAsync(c => c.Id == id);
                return View(chitiet);
            }
        }

        // GET: ChitietNhaps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var chitietNhap = await _context.ChitietNhaps
                .Include(c => c.Nhap)
                    .ThenInclude(n => n.CuaNhap)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (chitietNhap == null) return NotFound();
            return View(chitietNhap);
        }

        // POST: ChitietNhaps/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chitietNhap = await _context.ChitietNhaps.FindAsync(id);
            if (chitietNhap != null)
            {
                _context.ChitietNhaps.Remove(chitietNhap);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ChitietNhapExists(int id)
        {
            return (_context.ChitietNhaps?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
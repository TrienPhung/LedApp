using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using Microsoft.AspNetCore.Authorization;
using LedApp.Data;

namespace LedApp.Controllers
{
    [Authorize]
    public class CanhBaosController : Controller
    {
        private readonly ApplicationDBContext _context;

        public CanhBaosController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.CanhBaos.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var canhBao = await _context.CanhBaos.FirstOrDefaultAsync(m => m.Id == id);
            if (canhBao == null) return NotFound();
            return View(canhBao);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Bind("LoaiPhieu,PhieuId,LoaiCanhBao,ThoiGian,GhiChu")] CanhBao canhBao)
        {
            try
            {
                _context.Add(canhBao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch { return View(canhBao); }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var canhBao = await _context.CanhBaos.FindAsync(id);
            if (canhBao == null) return NotFound();
            return View(canhBao);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LoaiPhieu,PhieuId,LoaiCanhBao,ThoiGian,GhiChu")] CanhBao canhBao)
        {
            if (id != canhBao.Id) return NotFound();
            try
            {
                var existing = await _context.CanhBaos.FindAsync(id);
                if (existing == null) return NotFound();
                existing.LoaiPhieu = canhBao.LoaiPhieu;
                existing.PhieuId = canhBao.PhieuId;
                existing.LoaiCanhBao = canhBao.LoaiCanhBao;
                existing.ThoiGian = canhBao.ThoiGian;
                existing.GhiChu = canhBao.GhiChu;
                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CanhBaos.Any(e => e.Id == id)) return NotFound();
                return View(canhBao);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var canhBao = await _context.CanhBaos.FirstOrDefaultAsync(m => m.Id == id);
            if (canhBao == null) return NotFound();
            return View(canhBao);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var canhBao = await _context.CanhBaos.FindAsync(id);
            if (canhBao != null) _context.CanhBaos.Remove(canhBao);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
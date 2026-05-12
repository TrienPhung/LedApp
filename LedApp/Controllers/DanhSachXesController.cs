using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LedApp.Models;
using LedApp.Data;
using LedApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace LedApp.Controllers
{
    public class DanhSachXesController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<SignalServer> _hubContext;

        public DanhSachXesController(
            ApplicationDBContext context,
            UserManager<AppUser> userManager,
            IHubContext<SignalServer> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // Helper: lấy danh sách nhân viên có role TaiXe
        private async Task<List<nguoiDungs>> GetDanhSachTaiXe()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(nameof(Quyen.TaiXe));
            var userIds = usersInRole.Select(u => u.Id).ToList();
            return await _context.nguoiDungs
                .Where(n => n.UserId != null && userIds.Contains(n.UserId))
                .ToListAsync();
        }

        // GET: DanhSachXes
        public async Task<IActionResult> Index()
        {
            var danhSachXe = _context.DanhSachXes.Include(x => x.TaiXe);
            return View(await danhSachXe.ToListAsync());
        }

        // GET: DanhSachXes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (xe == null) return NotFound();
            return View(xe);
        }

        // GET: DanhSachXes/Create
        public async Task<IActionResult> Create()
        {
            var taixe = await GetDanhSachTaiXe();
            ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName");
            return View();
        }

        // POST: DanhSachXes/Create
        [HttpPost]
        public async Task<IActionResult> Create(
            [Bind("Id,BienSoXe,LoaiXe,TaiTrong,TrangThai,GhiChu,TaiXeId")] DanhSachXe xe)
        {
            try
            {
                _context.Add(xe);
                await _context.SaveChangesAsync();
                await PushTongHopXe();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                var taixe = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
                return View(xe);
            }
        }

        // GET: DanhSachXes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes.FindAsync(id);
            if (xe == null) return NotFound();
            var taixe = await GetDanhSachTaiXe();
            ViewData["TaiXeId"] = new SelectList(taixe, "Id", "FullName", xe.TaiXeId);
            return View(xe);
        }

        // POST: DanhSachXes/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,BienSoXe,LoaiXe,TaiTrong,TrangThai,GhiChu,TaiXeId")] DanhSachXe xe)
        {
            if (id != xe.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["Error"] = string.Join(" | ", errors);
                var taixeList = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixeList, "Id", "FullName", xe.TaiXeId);
                return View(xe);
            }

            try
            {
                _context.Update(xe);
                await _context.SaveChangesAsync();
                await PushTongHopXe();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                var taixeList = await GetDanhSachTaiXe();
                ViewData["TaiXeId"] = new SelectList(taixeList, "Id", "FullName", xe.TaiXeId);
                return View(xe);
            }
        }

        // GET: DanhSachXes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var xe = await _context.DanhSachXes
                .Include(x => x.TaiXe)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (xe == null) return NotFound();
            return View(xe);
        }

        // POST: DanhSachXes/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var xe = await _context.DanhSachXes.FindAsync(id);
                if (xe != null)
                {
                    if (xe.TrangThai == (int)TrangThaiXe.DangPhanCong ||
                        xe.TrangThai == (int)TrangThaiXe.DangVanChuyen)
                    {
                        TempData["Error"] = "Không thể xóa xe đang hoạt động!";
                        return RedirectToAction(nameof(Index));
                    }
                    _context.DanhSachXes.Remove(xe);
                    await _context.SaveChangesAsync();
                    await PushTongHopXe();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: DanhSachXes/BulkDelete
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteXeRequest request)
        {
            if (request?.Ids == null || !request.Ids.Any())
                return BadRequest(new { message = "Không có ID nào được gửi lên." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var skipped = new List<string>();

                foreach (var id in request.Ids)
                {
                    var xe = await _context.DanhSachXes.FindAsync(id);
                    if (xe == null) continue;

                    if (xe.TrangThai == (int)TrangThaiXe.DangPhanCong ||
                        xe.TrangThai == (int)TrangThaiXe.DangVanChuyen)
                    {
                        skipped.Add(xe.BienSoXe ?? $"#{id}");
                        continue;
                    }

                    _context.DanhSachXes.Remove(xe);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                await PushTongHopXe();

                var deleted = request.Ids.Count - skipped.Count;
                var msg = $"Đã xóa {deleted} xe.";
                if (skipped.Any())
                    msg += $" Bỏ qua {skipped.Count} xe đang hoạt động: {string.Join(", ", skipped)}.";

                return Ok(new { message = msg, deleted, skipped = skipped.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Helper: push tổng hợp xe qua SignalR
        private async Task PushTongHopXe()
        {
            var tatCa = await _context.DanhSachXes.ToListAsync();

            var result = new
            {
                TrongBai = tatCa.Count(x => x.TrangThai == (int)TrangThaiXe.TrongBai),
                DangPhanCong = tatCa.Count(x => x.TrangThai == (int)TrangThaiXe.DangPhanCong),
                DangVanChuyen = tatCa.Count(x => x.TrangThai == (int)TrangThaiXe.DangVanChuyen),
                BaoTri = tatCa.Count(x => x.TrangThai == (int)TrangThaiXe.BaoTri),
                TongSo = tatCa.Count
            };

            await _hubContext.Clients.All.SendAsync("UpdateTongHopXe", result);
        }

        private bool DanhSachXeExists(int id)
        {
            return (_context.DanhSachXes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }

    public class BulkDeleteXeRequest
    {
        public List<int> Ids { get; set; } = new();
    }
}
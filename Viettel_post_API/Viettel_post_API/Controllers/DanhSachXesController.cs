using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Viettel_post_API.Data;
using Viettel_post_API.Hubs;
using Viettel_post_API.Models;

namespace Viettel_post_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DanhSachXesController : ControllerBase
    {
        private readonly Viettel_post_APIContext _context;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public DanhSachXesController(Viettel_post_APIContext context, IHubContext<SignalServer> hubContext, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
        }
        // Helper method
        private async Task NotifyLedApp()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("LedApp");
                var response = await client.PostAsync("ImportFromApi/Notify", null);
                Console.WriteLine($"✅ NotifyLedApp: {response.StatusCode}"); // thêm dòng này
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ NotifyLedApp lỗi: {ex.Message}"); // thêm dòng này
            }
        }
        [HttpPut("chuyen/{chuyenId}/ngaydukien")]
        public async Task<IActionResult> CapNhatNgayDuKien(int chuyenId, [FromBody] DateTime ngayDuKien)
        {
            var chuyen = await _context.ChuyenXes.FindAsync(chuyenId);
            if (chuyen == null) return NotFound($"Không tìm thấy chuyến {chuyenId}");

            // Điều kiện: ngày dự kiến không được trước ngày tạo
            if (ngayDuKien < chuyen.NgayTao)
            {
                return BadRequest("Ngày dự kiến không được trước ngày tạo chuyến.");
            }

            // Điều kiện: không được set quá xa trong quá khứ
            if (ngayDuKien < DateTime.Today)
            {
                return BadRequest("Ngày dự kiến phải từ hôm nay trở đi.");
            }

            chuyen.NgayDuKien = ngayDuKien;
            _context.Entry(chuyen).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            // 🔥 THÔNG BÁO
            await NotifyLedApp();

            return Ok(new { chuyenId, ngayDuKien = chuyen.NgayDuKien });
        }
        [HttpPut("chuyen/{chuyenId}/trangthai")]
        public async Task<IActionResult> CapNhatTrangThai(int chuyenId, [FromBody] int trangThai)
        {
            var chuyen = await _context.ChuyenXes.FindAsync(chuyenId);
            if (chuyen == null) return NotFound($"Không tìm thấy chuyến {chuyenId}");

            var trangThaiMoi = (TrangThaiChuyen)trangThai;

            // ✅ Không cho phép downgrade trạng thái
            if ((chuyen.TrangThai == TrangThaiChuyen.DaDen || chuyen.TrangThai == TrangThaiChuyen.HoanThanh)
                && (trangThaiMoi == TrangThaiChuyen.ChuaVe || trangThaiMoi == TrangThaiChuyen.DangVe))
            {
                return BadRequest("Không thể cập nhật ngược trạng thái khi chuyến đã đến hoặc hoàn thành.");
            }

            // Cập nhật trạng thái
            chuyen.TrangThai = trangThaiMoi;

            // ✅ Tự động cập nhật thời gian theo trạng thái
            switch (trangThaiMoi)
            {
                case TrangThaiChuyen.DaDen:
                case TrangThaiChuyen.DangNhapHang:
                    chuyen.ThoiGianDen ??= DateTime.Now;
                    break;
                case TrangThaiChuyen.HoanThanh:
                    chuyen.ThoiGianDen ??= DateTime.Now;
                    chuyen.ThoiGianHoanThanh = DateTime.Now;
                    break;
            }

            _context.Entry(chuyen).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            // 🔥 THÔNG BÁO
            await NotifyLedApp();

            return Ok(new
            {
                chuyenId,
                trangThai = (int)chuyen.TrangThai,
                thoiGianDen = chuyen.ThoiGianDen,
                thoiGianHoanThanh = chuyen.ThoiGianHoanThanh
            });
        }

        // GET: api/DanhSachXes/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var data = await _context.DanhSachXes
                .Select(xe => new
                {
                    xe.Id,
                    xe.BienSo,
                    xe.TenLaiXe,
                    xe.MaChiNhanh,
                    ChuyenHienTai = xe.ChuyenXes
                        .OrderBy(c => c.TrangThai == TrangThaiChuyen.DaDen ? 0 :
                                      c.TrangThai == TrangThaiChuyen.DangNhapHang ? 1 :
                                      c.TrangThai == TrangThaiChuyen.DangVe ? 2 :
                                      c.TrangThai == TrangThaiChuyen.ChuaVe ? 3 : 4)
                        .ThenByDescending(c => c.NgayDuKien)
                        .Select(c => new
                        {
                            c.Id,
                            c.MaChuyenApi,
                            c.LanThu,
                            c.NgayDuKien,
                            c.TrangThai,
                            c.ThoiGianDen,
                            c.ThoiGianHoanThanh,
                            c.IsLatest,
                            HangHoas = c.HangHoas.Select(h => new
                            {
                                h.DonVi,
                                h.SoLuong
                            })
                        })
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.ChuyenHienTai.NgayDuKien)
                .ToListAsync();

            return Ok(data);
        }
        // GET: api/DanhSachXes/1/lich-su
        [HttpGet("{id}/lich-su")]
        public async Task<IActionResult> GetLichSu(int id)
        {
            var data = await _context.ChuyenXes
                .Where(c => c.DanhSachXeId == id)
                .OrderByDescending(c => c.NgayDuKien)
                .Select(c => new
                {
                    c.Id,
                    c.MaChuyenApi,
                    c.LanThu,
                    c.NgayDuKien,
                    c.TrangThai,
                    c.ThoiGianDen,
                    c.ThoiGianHoanThanh,
                     HangHoas = c.HangHoas.Select(h => new  // ✅ thêm
                     {
                         h.DonVi,
                         h.SoLuong
                     })
                })
                .ToListAsync();

            return Ok(data);
        }
        // GET: api/DanhSachXes/chuyen/5/hanghoa
        [HttpGet("chuyen/{chuyenId}/hanghoa")]
        public async Task<IActionResult> GetHangHoa(int chuyenId)
        {
            var data = await _context.HangHoaItems
                .Where(h => h.ChuyenXeId == chuyenId)
                .Select(h => new
                {
                    h.DonVi,
                    h.SoLuong
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/DanhSachXes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DanhSachXe>>> GetDanhSachXe()
        {
          if (_context.DanhSachXes == null)
          {
              return NotFound();
          }
            return await _context.DanhSachXes.ToListAsync();
        }

        // GET: api/DanhSachXes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DanhSachXe>> GetDanhSachXe(int id)
        {
          if (_context.DanhSachXes == null)
          {
              return NotFound();
          }
            var danhSachXe = await _context.DanhSachXes.FindAsync(id);

            if (danhSachXe == null)
            {
                return NotFound();
            }

            return danhSachXe;
        }

        // PUT: api/DanhSachXes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDanhSachXe(int id, DanhSachXe danhSachXe)
        {
            if (id != danhSachXe.Id)
            {
                return BadRequest();
            }

            _context.Entry(danhSachXe).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DanhSachXeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/DanhSachXes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DanhSachXe>> PostDanhSachXe(DanhSachXe danhSachXe)
        {
          if (_context.DanhSachXes == null)
          {
              return Problem("Entity set 'Viettel_post_APIContext.DanhSachXes'  is null.");
          }
            _context.DanhSachXes.Add(danhSachXe);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDanhSachXe", new { id = danhSachXe.Id }, danhSachXe);
        }

        // DELETE: api/DanhSachXes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDanhSachXe(int id)
        {
            if (_context.DanhSachXes == null)
            {
                return NotFound();
            }
            var danhSachXe = await _context.DanhSachXes.FindAsync(id);
            if (danhSachXe == null)
            {
                return NotFound();
            }
            
            _context.DanhSachXes.Remove(danhSachXe);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DanhSachXeExists(int id)
        {
            return (_context.
                DanhSachXes?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

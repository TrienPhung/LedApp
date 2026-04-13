using LedApp.Data;
using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class XuatRepository
    {
        private readonly ApplicationDBContext _dbContext;

        public XuatRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // FIX: chỉ lấy phiếu đang THỰC SỰ hoạt động tại cửa
        // HoanThanh(3) và DaXuatPhat(4) → cửa về trống, trả null
        public Xuat? GetXuat(int? cuaXuatId)
        {
            var trangThaiHoatDong = new[]
            {
                (int)TrangThaiXuat.DaPhanCong,   // 0 — chờ xe vào
                (int)TrangThaiXuat.DangBanGiao,  // 1 — đang bàn giao
                (int)TrangThaiXuat.QuaThoiGian   // 2 — quá giờ nhưng vẫn còn ở cửa
            };

            return _dbContext.Xuats
                .Include(s => s.Xe)
                .Where(s => s.CuaXuatId == cuaXuatId
                    && s.ThoiGianPhanCong.Date == DateTime.Today
                    && trangThaiHoatDong.Contains(s.TrangThai))
                .OrderByDescending(s => s.ThoiGianPhanCong)
                .FirstOrDefault();
        }

        public List<Xuat> GetAllXuat()
        {
            var result = _dbContext.Xuats
                .Where(s => s.ThoiGianPhanCong.Date == DateTime.Today
                    && s.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                .Include(s => s.ChitietXuats)
                .Include(s => s.Xe)
                .Take(20)
                .ToList();

            foreach (var item in result)
                item.ChitietXuats ??= new List<ChitietXuat>();

            return result;
        }

        public List<DanhSachXe> GetXeRanh()
        {
            return _dbContext.DanhSachXes
                .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai)
                .ToList();
        }
    }
}
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

        // Lấy phiếu xuất đang hoạt động theo cửa (chưa DaXuatPhat)
        public Xuat? GetXuat(int? cuaXuatId)
        {
            return _dbContext.Xuats
                .Where(s => s.CuaXuatId == cuaXuatId
                    && s.ThoiGianPhanCong.Date == DateTime.Today
                    && s.TrangThai != (int)TrangThaiXuat.DaXuatPhat)
                .OrderByDescending(s => s.ThoiGianPhanCong)
                .FirstOrDefault();
        }

        // Lấy tất cả phiếu xuất trong ngày chưa DaXuatPhat
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
            {
                item.ChitietXuats ??= new List<ChitietXuat>();
            }
            return result;
        }

        // Lấy xe rảnh trong bãi (TrongBai và chưa được phân công hôm nay)
        public List<DanhSachXe> GetXeRanh()
        {
            return _dbContext.DanhSachXes
                .Where(x => x.TrangThai == (int)TrangThaiXe.TrongBai)
                .ToList();
        }
    }
}
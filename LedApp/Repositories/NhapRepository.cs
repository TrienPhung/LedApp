using LedApp.Data;
using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class NhapRepository
    {
        private readonly ApplicationDBContext _dbContext;

        public NhapRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Nhap? GetNhap(int? cuaNhapId)
        {
            return _dbContext.Nhaps
                .Where(s => s.CuaNhapId == cuaNhapId
                    && s.ThoiGianPhanCong.Date == DateTime.Today
                    && s.TrangThai != (int)TrangThaiNhap.HoanThanh)
                .FirstOrDefault();
        }

        //public List<Nhap> GetAllNhap()
        //{
        //    return _dbContext.Nhaps.Where(s => s.ThoiGianPhanCong.Date == DateTime.Today && s.TrangThai != "HoanThanh").Include(s => s.ChitietNhaps).Take(20).ToList();
        //}
        public List<Nhap> GetAllNhap()
        {
            var result = _dbContext.Nhaps
                .Where(s => s.ThoiGianPhanCong.Date == DateTime.Today
                    && s.TrangThai != (int)TrangThaiNhap.HoanThanh)
                .Include(s => s.ChitietNhaps)
                .Take(20)
                .ToList();

            // Đảm bảo ChitietNhaps không null
            foreach (var item in result)
            {
                item.ChitietNhaps ??= new List<ChitietNhap>();
            }

            return result;
        }
    }
}
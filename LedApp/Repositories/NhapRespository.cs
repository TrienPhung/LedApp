using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class NhapRespository
    {
        private readonly ApplicationDBContext dbContext;
        public NhapRespository(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public Nhap? GetNhap(int? cuanhapid)
        {
            return dbContext.Nhaps.Where(s => s.CuaNhapId == cuanhapid && s.NgayNhap == DateTime.Today && s.TrangThai == false).FirstOrDefault(); // 
        }
        public List<Nhap> GetAllNhap()
        {
            return dbContext.Nhaps.Where(s => s.NgayNhap == DateTime.Today && s.TrangThai == false).Include(s => s.ChitietNhaps).Take(20).ToList();
        }
    }
}

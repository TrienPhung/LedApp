using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class xuatRespository
    {
        private readonly ApplicationDBContext dbContext;
        public xuatRespository(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public dulieuxuat? GetXuat(int? cuaxuatid)
        {
            return dbContext.dulieuxuats.Where(s => s.CuaXuatId==cuaxuatid && s.NgayXuat == DateTime.Today && s.TrangThai == false).FirstOrDefault();
        }
        public List<dulieuxuat> GetAllXuat()
        {
            return dbContext.dulieuxuats.Where(s=>s.NgayXuat==DateTime.Today && s.TrangThai==false).Include(s=>s.ChitietXuat).Take(20).ToList();
        }
    }
}

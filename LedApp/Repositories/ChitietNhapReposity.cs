using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class ChitietNhapReposity
    {
        private readonly ApplicationDBContext dbContext;
        public ChitietNhapReposity(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public List<ChitietNhap> GetChiTietNhap(int? nhapid)
        {
            // var x = _context.Xuats.Where(s=>s.CuaXuat.Equals("01")).FirstOrDefault();
            return dbContext.ChitietNhaps.Where(s => s.NhapId == nhapid).Take(4).ToList();
        }
    }
}

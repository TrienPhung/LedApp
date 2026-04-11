using LedApp.Data;
using LedApp.Models;

namespace LedApp.Repositories
{
    public class ChitietXuatRepository
    {
        private readonly ApplicationDBContext _context;

        public ChitietXuatRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public List<ChitietXuat> GetChitietXuat(int? xuatId)
        {
            // var x = _context.Xuats.Where(s=>s.CuaXuat.Equals("01")).FirstOrDefault();
            return _context.ChitietXuats
                .Where(s => s.XuatId == xuatId)
                .Take(4)
                .ToList();
        }

        //public Xuat GetXuatThaoCua(string tencuaxuat)
        //{
        //    return _context.Xuats.Where(s => s.TenCuaXuat.Equals(tencuaxuat)).FirstOrDefault();
        //}
    }
}
using LedApp.Models;

namespace LedApp.Repositories
{
    public class xexuatRepository
    {
        private readonly ApplicationDBContext _context;
        public xexuatRepository(ApplicationDBContext context)
        {
            this._context = context;
        }
        public List<ChitietXuat> Getxexuat(int? xuatid)
        {
           // var x = _context.Xuats.Where(s=>s.CuaXuat.Equals("01")).FirstOrDefault();
            return _context.ChitietXuats.Where(s=>s.dulieuxuatId==xuatid).Take(4).ToList();
        }
        //public Xuat GetXuatThaoCua(string tencuaxuat)
        //{
        //    return _context.Xuats.Where(s => s.TenCuaXuat.Equals(tencuaxuat)).FirstOrDefault();
        //}
    }
}

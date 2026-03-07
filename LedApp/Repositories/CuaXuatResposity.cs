using LedApp.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LedApp.Repositories
{ 
    public class CuaXuatResposity
    {
        private readonly ApplicationDBContext _context;
        public CuaXuatResposity(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task capnhat(CuaXuat cuaXuat)
        {
            _context.Entry(cuaXuat).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _context.SaveChangesAsync();
        }
        public int CuaXuatSL()
        {
            int sl = 0;
            try
            {
                sl = int.Parse(_context.CauHinhs.Where(s => s.Key.Equals("CuaXuatSL")).FirstOrDefault().Value);
                return sl;
            }
            catch (Exception)
            {
                return sl;
            }
        }
    }
}

using LedApp.Data;
using LedApp.Models;

namespace LedApp.Repositories
{
    public class CuaXuatRepository
    {
        private readonly ApplicationDBContext _context;

        public CuaXuatRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task CapNhat(CuaXuat cuaXuat)
        {
            _context.Entry(cuaXuat).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public int CuaXuatSL()
        {
            var cauHinh = _context.CauHinhs
                .FirstOrDefault(s => s.Key == "CuaXuatSL");

            if (cauHinh == null) return 0;

            return int.TryParse(cauHinh.Value, out int sl) ? sl : 0;
        }
    }
}
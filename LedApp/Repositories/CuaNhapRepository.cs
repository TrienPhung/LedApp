using LedApp.Data;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class CuaNhapRepository
    {
        private readonly ApplicationDBContext _dbContext;

        public CuaNhapRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public int CuaNhapSL()
        {
            var cauHinh = _dbContext.CauHinhs
                .FirstOrDefault(s => s.Key == "CuaNhapSL");

            if (cauHinh == null) return 0;

            return int.TryParse(cauHinh.Value, out int sl) ? sl : 0;
        }
    }
}
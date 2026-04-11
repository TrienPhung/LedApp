using LedApp.Data;
using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class ChitietNhapRepository
    {
        private readonly ApplicationDBContext _dbContext;

        public ChitietNhapRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<ChitietNhap> GetChiTietNhap(int? nhapId)
        {
            return _dbContext.ChitietNhaps
                .Where(s => s.NhapId == nhapId)
                .Take(4)
                .ToList();
        }
    }
}
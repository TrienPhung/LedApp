using LedApp.Data;
using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDBContext _dbContext;

        public UserRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<nguoiDungs?> GetNguoiDung(string tenDangNhap, string matKhau)
        {
            return await _dbContext.nguoiDungs
                .FirstOrDefaultAsync(s => s.Username == tenDangNhap && s.Password == matKhau);
        }
    }
}
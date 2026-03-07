using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class UserRepository
    {
        private readonly ApplicationDBContext dbcontext;
        public UserRepository(ApplicationDBContext dbcontext)
        {

            this.dbcontext = dbcontext;
        }
        public async Task<NguoiDung?> GetNguoiDung(string tendangnhap, string matkhau)
        {
            return await dbcontext.nguoiDungs.FirstOrDefaultAsync(s => s.Username == tendangnhap && s.Password == matkhau);
        }
    }
}

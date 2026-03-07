using LedApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Repositories
{
    public class cuanhapResposity
    {
        private readonly ApplicationDBContext dbContext;
        public cuanhapResposity(ApplicationDBContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public int CuaNhapSL()
        {
            int sl = 0;
            try
            {
                sl = int.Parse(dbContext.CauHinhs.Where(s => s.Key.Equals("CuaNhapSL")).FirstOrDefault().Value);
                
            }
            catch (Exception)
            {
              
            }
            return sl;
        }
    }
}

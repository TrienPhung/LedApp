using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Models
{
    public class ApplicationDBContext: IdentityDbContext<AppUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
        #region
        public DbSet<Nhap> Nhaps { get; set; }
        public DbSet<ChitietNhap> ChitietNhaps { get; set; }
        public DbSet<CuaNhap> CuaNhaps { get; set;}
        
        public DbSet<dulieuxuat> dulieuxuats { get; set; }
        public DbSet<ChitietXuat> ChitietXuats { get;set; }
        public DbSet<CuaXuat> CuaXuats { get; set; }
        public DbSet<NguoiDung> nguoiDungs { get; set; }
        public DbSet<CauHinh> CauHinhs { get; set; }

        #endregion
    }
}

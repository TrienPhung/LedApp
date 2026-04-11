using LedApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Data
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
        // ✅ Thêm/sửa các dòng mới
        public DbSet<DanhSachXe> DanhSachXes { get; set; }
        public DbSet<CuaXuat> CuaXuats { get; set; }
        public DbSet<Xuat> Xuats { get; set; }
        public DbSet<ChitietXuat> ChitietXuats { get; set; }

        // Nhập
        public DbSet<CuaNhap> CuaNhaps { get; set; }
        public DbSet<Nhap> Nhaps { get; set; }
        public DbSet<ChitietNhap> ChitietNhaps { get; set; }

        // Dùng chung
        public DbSet<LichSuBanGiao> LichSuBanGiaos { get; set; }
        public DbSet<CanhBao> CanhBaos { get; set; }
        public DbSet<CauHinh> CauHinhs { get; set; }
        public DbSet<nguoiDungs> nguoiDungs { get; set; }
        #endregion
    }
}

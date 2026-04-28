using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Viettel_post_API.Models;

namespace Viettel_post_API.Data
{
    public class Viettel_post_APIContext : DbContext
    {
        public Viettel_post_APIContext (DbContextOptions<Viettel_post_APIContext> options)
            : base(options)
        {
        }

        public DbSet<DanhSachXe>? DanhSachXes { get; set; } = default!;
        public DbSet<ChuyenXe>? ChuyenXes { get; set; } = default!;
        public DbSet<HangHoaItem> HangHoaItems { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 🔥 Không cho trùng chuyến từ API
            modelBuilder.Entity<ChuyenXe>()
                .HasIndex(x => x.MaChuyenApi)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Viettel_post_API.Hubs;
using Viettel_post_API.Models;
using Microsoft.Extensions.DependencyInjection;
using Viettel_post_API.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Viettel_post_APIContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Viettel_post_APIContext") ?? throw new InvalidOperationException("Connection string 'Viettel_post_APIContext' not found.")));
// Đăng ký Controllers
builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
// Đăng ký Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đăng ký SignalR
builder.Services.AddSignalR();
builder.Services.AddHttpClient("LedApp", client =>
{
    client.BaseAddress = new Uri("https://localhost:7266/");
});

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Viettel_post_API.Data.Viettel_post_APIContext>();

    if (!context.DanhSachXes.Any())
    {
        // 🔥 TẠO XE
        var xe1 = new DanhSachXe
        {
            BienSo = "29A-12345",
            TenLaiXe = "Nguyễn Văn A",
            MaChiNhanh = "HN"
        };

        var xe2 = new DanhSachXe
        {
            BienSo = "30B-67890",
            TenLaiXe = "Trần Văn B",
            MaChiNhanh = "HN"
        };

        context.DanhSachXes.AddRange(xe1, xe2);
        context.SaveChanges();

        // 🔥 TẠO CHUYẾN
        var chuyenXeList = new List<ChuyenXe>
        {
            // Xe 1 - chuyến cũ
            new ChuyenXe
            {
                DanhSachXeId = xe1.Id,
                MaChuyenApi = "CX001",
                LanThu = 1,
                NgayDuKien = DateTime.Today.AddHours(-5),
                ThoiGianDen = DateTime.Now.AddHours(-4),
                ThoiGianHoanThanh = DateTime.Now.AddHours(-3),
                TrangThai = TrangThaiChuyen.HoanThanh,
                IsLatest = false
            },

            // Xe 1 - chuyến mới (đang chạy)
            new ChuyenXe
            {
                DanhSachXeId = xe1.Id,
                MaChuyenApi = "CX002",
                LanThu = 2,
                NgayDuKien = DateTime.Today,
                TrangThai = TrangThaiChuyen.DangVe,
                IsLatest = true
            },

            // Xe 2 - chuyến hiện tại
            new ChuyenXe
            {
                DanhSachXeId = xe2.Id,
                MaChuyenApi = "CX003",
                LanThu = 1,
                NgayDuKien = DateTime.Today,
                TrangThai = TrangThaiChuyen.DangNhapHang,
                IsLatest = true
            }
        };

        context.ChuyenXes.AddRange(chuyenXeList);
        context.SaveChanges();

        // 🔥 TẠO HÀNG HÓA
        var hangHoaList = new List<HangHoaItem>
        {
            // Hàng của chuyến 1
            new HangHoaItem
            {
                ChuyenXeId = chuyenXeList[0].Id,
                DonVi = "Kiện",
                SoLuong = 100
            },

            // Hàng của chuyến 2
            new HangHoaItem
            {
                ChuyenXeId = chuyenXeList[1].Id,
                DonVi = "Kiện",
                SoLuong = 50
            },

            // Hàng của xe 2
            new HangHoaItem
            {
                ChuyenXeId = chuyenXeList[2].Id,
                DonVi = "Bao",
                SoLuong = 200
            }
        };

        context.HangHoaItems.AddRange(hangHoaList);
        context.SaveChanges();
    }
}
// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

// Map Hub SignalR
app.MapHub<SignalServer>("/SignalServer");


app.Run();

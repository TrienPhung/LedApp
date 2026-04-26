using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Services
{
    public class QuaHanNhapService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<QuaHanNhapService> _logger;

        public QuaHanNhapService(
            IServiceScopeFactory scopeFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<QuaHanNhapService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        // QuaHanNhapService.cs

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await KiemTraQuaHan();
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task KiemTraQuaHan()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var now = DateTime.Now;

                // ✅ Buffer 30 giây — chỉ xử lý xe đã quá giờ ít nhất 30 giây
                // Tránh conflict với client timer đang đếm ngược
                var nguong = now.AddSeconds(-30);

                var quaHans = await db.Nhaps
                    .Where(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao
                             && n.ThoiGianGioiHan != null
                             && n.ThoiGianGioiHan < nguong) // ← nguong thay vì now
                    .ToListAsync();

                foreach (var nhap in quaHans)
                {
                    nhap.TrangThai = (int)TrangThaiNhap.QuaThoiGian;

                    var daCoCanh = await db.CanhBaos
                        .AnyAsync(c => c.PhieuId == nhap.Id
                                    && c.LoaiCanhBao == "QuaHanNhap"
                                    && c.LoaiPhieu == "NHAP");

                    if (!daCoCanh)
                    {
                        db.CanhBaos.Add(new CanhBao
                        {
                            LoaiPhieu = "NHAP",
                            PhieuId = nhap.Id,
                            LoaiCanhBao = "QuaHanNhap",
                            ThoiGian = now,
                            GhiChu = $"Xe:{nhap.BienSoXe}|Cua:{nhap.CuaNhapId}|GioiHan:{nhap.ThoiGianGioiHan:HH:mm}",
                            TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                        });

                        _logger.LogWarning(
                            "Quá hạn nhập: nhapId={Id} bienSo={BienSo} cuaNhapId={CuaNhapId}",
                            nhap.Id, nhap.BienSoXe, nhap.CuaNhapId);
                    }
                }

                if (quaHans.Any())
                {
                    await db.SaveChangesAsync();

                    var chuaXuLy = await db.CanhBaos
                        .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                    await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                    foreach (var nhap in quaHans)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivedNhap", new
                        {
                            bienSoXe = nhap.BienSoXe,
                            thoiGianVaoCua = nhap.ThoiGianVaoCua,
                            thoiGianGioiHan = nhap.ThoiGianGioiHan,
                            trangThai = nhap.TrangThai
                        }, nhap.CuaNhapId);
                    }

                    // ✅ Push để DieuDoNhap reload bảng
                    await _hubContext.Clients.All.SendAsync("TrangThaiXeUpdated", 0, 2, (string?)null, (string?)null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi KiemTraQuaHan");
            }
        }
    }
}
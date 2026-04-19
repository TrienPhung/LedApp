using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LedApp.Services
{
    public class XuatCanhBaoService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly ILogger<XuatCanhBaoService> _logger;

        public XuatCanhBaoService(
            IServiceScopeFactory scopeFactory,
            IHubContext<SignalServer> hubContext,
            ILogger<XuatCanhBaoService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("XuatCanhBaoService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await KiemTraQuaGio();
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task KiemTraQuaGio()
        {
            try
            {
                // Dùng scope vì DbContext không phải singleton
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var now = DateTime.Now;
                var today = DateTime.Today;

                // Tìm phiếu đang bàn giao và đã quá giờ giới hạn
                var phieuQuaGio = await context.Xuats
                    .Where(x => x.ThoiGianPhanCong.Date == today
                             && x.TrangThai == (int)TrangThaiXuat.DangBanGiao
                             && x.ThoiGianGioiHan.HasValue
                             && x.ThoiGianGioiHan.Value < now)
                    .ToListAsync();

                if (!phieuQuaGio.Any()) return;

                foreach (var xuat in phieuQuaGio)
                {
                    xuat.TrangThai = (int)TrangThaiXuat.QuaThoiGian;

                    context.CanhBaos.Add(new CanhBao
                    {
                        LoaiPhieu = "XUAT",
                        PhieuId = xuat.Id,
                        LoaiCanhBao = "QuaThoiGian",
                        ThoiGian = now,
                        GhiChu = $"Xe quá thời gian xuất tại cửa {xuat.CuaXuatId}"
                    });
                }

                await context.SaveChangesAsync();

                // Push SignalR cảnh báo đỏ cho từng phiếu
                foreach (var xuat in phieuQuaGio)
                {
                    await _hubContext.Clients.All.SendAsync("CanhBaoDo", new
                    {
                        LoaiPhieu = "XUAT",
                        PhieuId = xuat.Id,
                        CuaXuatId = xuat.CuaXuatId,
                        LoaiCanhBao = "QuaThoiGian",
                        ThoiGian = now
                    });

                    _logger.LogWarning(
                        "Phiếu xuất #{Id} cửa {CuaId} quá thời gian!",
                        xuat.Id, xuat.CuaXuatId);
                }

                // Push cập nhật bảng tổng hợp
                await _hubContext.Clients.All.SendAsync("UpdateBangTongHopXuat", new { });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XuatCanhBaoService.KiemTraQuaGio");
            }
        }
    }
}
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
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task KiemTraQuaGio()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var now = DateTime.Now;
                var today = DateTime.Today;

                // Tìm phiếu quá giờ nhưng CHƯA được nhân viên báo
                // (tức là vẫn còn TrangThai = DangBanGiao, chưa chuyển sang QuaThoiGian)
                var phieuQuaGio = await context.Xuats
                      .Include(x => x.Xe)
                    .Where(x => x.ThoiGianPhanCong.Date == today
                             && x.TrangThai == (int)TrangThaiXuat.DangBanGiao
                             && x.ThoiGianGioiHan.HasValue
                             && x.ThoiGianGioiHan.Value < now)
                    .ToListAsync();

                if (!phieuQuaGio.Any()) return;

                foreach (var xuat in phieuQuaGio)
                {
                    var daCoCanh = await context.CanhBaos
                        .AnyAsync(c => c.PhieuId == xuat.Id
                                    && c.LoaiPhieu == "XUAT"
                                    && c.LoaiCanhBao == "QuaHanXuat");
                    if (daCoCanh) continue;

                    xuat.TrangThai = (int)TrangThaiXuat.QuaThoiGian;

                    context.CanhBaos.Add(new CanhBao
                    {
                        LoaiPhieu = "XUAT",
                        PhieuId = xuat.Id,
                        LoaiCanhBao = "QuaHanXuat",
                        ThoiGian = now,
                        GhiChu = $"Xe:{xuat.Xe?.BienSoXe ?? xuat.XeId?.ToString()}|Cua:{xuat.CuaXuatId}|GioiHan:{xuat.ThoiGianGioiHan:HH:mm}",
                        TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                    });

                    await context.SaveChangesAsync();

                    // Chỉ push 1 lần duy nhất với đầy đủ thông tin
                    await _hubContext.Clients.All.SendAsync("ReceivedXuat", new
                    {
                        bienSoXe = xuat.Xe?.BienSoXe ?? "--",
                        trangThai = (int)TrangThaiXuat.QuaThoiGian,
                        thoiGianVaoCua = xuat.ThoiGianVaoCua,
                        thoiGianGioiHan = xuat.ThoiGianGioiHan
                    }, xuat.CuaXuatId);

                    await _hubContext.Clients.All.SendAsync("UpdateBangTongHopXuat", (object?)null);

                    var chuaXuLy = await context.CanhBaos
                        .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                    await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                    _logger.LogWarning("Backup: Phiếu #{Id} cửa {CuaId} quá giờ, background tự xử lý",
                        xuat.Id, xuat.CuaXuatId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi XuatCanhBaoService.KiemTraQuaGio");
            }
        }
    }
}
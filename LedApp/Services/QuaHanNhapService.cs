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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await KiemTraQuaHan();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // kiểm tra mỗi 1 phút
            }
        }

        private async Task KiemTraQuaHan()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var now = DateTime.Now;

                // Lấy các phiếu đang bàn giao đã quá giờ giới hạn
                var quaHans = await db.Nhaps
                    .Where(n => n.TrangThai == (int)TrangThaiNhap.DangBanGiao
                             && n.ThoiGianGioiHan != null
                             && n.ThoiGianGioiHan < now)
                    .ToListAsync();

                foreach (var nhap in quaHans)
                {
                    // ✅ Update TrangThai → QuaThoiGian
                    nhap.TrangThai = (int)TrangThaiNhap.QuaThoiGian;

                    // ✅ Kiểm tra đã có CanhBao QuaHan chưa (tránh insert 2 lần)
                    var daCoCanh = await db.CanhBaos
                        .AnyAsync(c => c.PhieuId == nhap.Id
                                    && c.LoaiCanhBao == "QuaHanNhap"
                                    && c.LoaiPhieu == "NHAP");

                    if (!daCoCanh)
                    {
                        var canhBao = new CanhBao
                        {
                            LoaiPhieu = "NHAP",
                            PhieuId = nhap.Id,
                            LoaiCanhBao = "QuaHanNhap",
                            ThoiGian = now,
                            GhiChu = $"Xe:{nhap.BienSoXe}|Cua:{nhap.CuaNhapId}|GioiHan:{nhap.ThoiGianGioiHan:HH:mm}",
                            TrangThai = (int)TrangThaiCanhBao.ChuaXuLy
                        };
                        db.CanhBaos.Add(canhBao);

                        _logger.LogWarning(
                            "Quá hạn nhập: nhapId={Id} bienSo={BienSo} cuaNhapId={CuaNhapId}",
                            nhap.Id, nhap.BienSoXe, nhap.CuaNhapId);
                    }
                }

                if (quaHans.Any())
                {
                    await db.SaveChangesAsync();

                    // SignalR → cập nhật badge chuông điều độ
                    var chuaXuLy = await db.CanhBaos
                        .CountAsync(c => c.TrangThai == (int)TrangThaiCanhBao.ChuaXuLy);
                    await _hubContext.Clients.All.SendAsync("UpdateCanhBaoBadge", chuaXuLy);

                    // SignalR → cập nhật từng cửa bị quá hạn
                    foreach (var nhap in quaHans)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceivedNhap", new
                        {
                            bienSoXe = nhap.BienSoXe,
                            thoiGianVaoCua = nhap.ThoiGianVaoCua,
                            thoiGianGioiHan = nhap.ThoiGianGioiHan,
                            trangThai = nhap.TrangThai  // = 2 (QuaThoiGian)
                        }, nhap.CuaNhapId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi KiemTraQuaHan");
            }
        }
    }
}
using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeChitietXuatTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<ChitietXuat> tableDependency;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public SubscribeChitietXuatTableDependency(
            IHubContext<SignalServer> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<ChitietXuat>(connectionString);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(ChitietXuat)} sqltableDependency error:{e.Error.Message}");
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<ChitietXuat> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var xuat = context.Xuats
                    .Include(x => x.Xe) 
                    .Where(s => s.Id == e.Entity.XuatId)
                    .FirstOrDefault();
                if (xuat != null)
                {
                    var chitiet = context.ChitietXuats
                        .Where(s => s.XuatId == xuat.Id)
                        .ToList();

                    var dto = new
                    {
                        id = xuat.Id,
                        cuaXuatId = xuat.CuaXuatId,
                        xeId = xuat.XeId,
                        bienSoXe = xuat.Xe?.BienSoXe ?? "--",
                        trangThai = xuat.TrangThai,
                        thoiGianVaoCua = xuat.ThoiGianVaoCua,
                        thoiGianGioiHan = xuat.ThoiGianGioiHan,
                        thoiGianHoanThanh = xuat.ThoiGianHoanThanh
                    };

                    _hubContext.Clients.All.SendAsync("Receivedxexuat", chitiet, xuat.CuaXuatId);
                    _hubContext.Clients.All.SendAsync("Receivedxuat", dto, xuat.CuaXuatId);
                }
            }
        }
    }
}
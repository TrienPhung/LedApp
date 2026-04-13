using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeNhapTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<Nhap> tableDependency;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public SubscribeNhapTableDependency(
            IHubContext<SignalServer> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<Nhap>(connectionString);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(Nhap)} error:{e.Error.Message}");
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<Nhap> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var nhap = context.Nhaps
                    .Where(n => n.Id == e.Entity.Id)
                    .FirstOrDefault();

                if (nhap != null)
                {
                    _hubContext.Clients.All.SendAsync("ReceivedNhap", nhap, nhap.CuaNhapId);
                }
            }
        }
    }
}
using LedApp.Data;
using LedApp.Hubs;
using LedApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeXuatTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<Xuat> tableDependency;
        private readonly IHubContext<SignalServer> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public SubscribeXuatTableDependency(
            IHubContext<SignalServer> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<Xuat>(connectionString);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(Xuat)} error:{e.Error.Message}");
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<Xuat> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                var xuat = context.Xuats
                    .Where(s => s.Id == e.Entity.Id)
                    .FirstOrDefault();

                if (xuat != null)
                {
                    _hubContext.Clients.All.SendAsync("Receivedxuat", xuat, xuat.CuaXuatId);
                }
            }
        }
    }
}
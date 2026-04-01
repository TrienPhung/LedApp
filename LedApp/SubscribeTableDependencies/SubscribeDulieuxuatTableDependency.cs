using LedApp.Hubs;
using LedApp.Models;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeDulieuxuatTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<dulieuxuat> tableDependency;
        SignalServer signalServer;

        public SubscribeDulieuxuatTableDependency(SignalServer signalServer)
        {
            this.signalServer = signalServer;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<dulieuxuat>(connectionString);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(dulieuxuat)} error:{e.Error.Message}");
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<dulieuxuat> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                // Cập nhật thông tin xe (giờ xuất, màu đỏ/trắng)
                signalServer.Sendxuat(e.Entity.CuaXuatId);
                // Cập nhật bảng tổng hợp
                signalServer.SendTongHopXuat();
            }
        }
    }
}
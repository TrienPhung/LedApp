using LedApp.Hubs;
using LedApp.Models;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeChitietXuatTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<ChitietXuat> tableDependency;
        SignalServer signalServer;
        public SubscribeChitietXuatTableDependency (SignalServer signalServer)
        {
            this.signalServer = signalServer;
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
                //signalServer.Sendxexuat();
                //signalServer.Sendxexuat2();
                //signalServer.Sendxexuat3();
                //signalServer.Sendxexuat4();
                //signalServer.Sendxexuat5();
                //signalServer.Sendxexuat6();

                // Cập nhật bảng chi tiết cửa xuất
                signalServer.Sendxexuat(e.Entity.XuatId);
                signalServer.Sendxuat(e.Entity.XuatId);
                //signalServer.SendTongHopXuat();
            }
        }
    }
}

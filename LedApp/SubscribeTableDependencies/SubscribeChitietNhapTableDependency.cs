using LedApp.Hubs;
using LedApp.Models;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeChitietNhapTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<ChitietNhap> tableDependency;
        SignalServer signalServer;

        public SubscribeChitietNhapTableDependency(SignalServer signalServer)
        {
            this.signalServer = signalServer;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<ChitietNhap>(connectionString);
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(ChitietNhap)} error:{e.Error.Message}");
        }

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<ChitietNhap> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
              //  signalServer.SendTongHopNhap();
                // Nếu muốn cập nhật bảng chi tiết cửa nhập thì thêm:
                // signalServer.SendChitietNhap(e.Entity.NhapId);
            }
        }
    }
}
using LedApp.Hubs;
using LedApp.Models;
using TableDependency.SqlClient;
using TableDependency.SqlClient.Base.EventArgs;

namespace LedApp.SubscribeTableDependencies
{
    public class SubscribeNhapTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<Nhap> tableDependency;
        SignalServer signalServer;

        public SubscribeNhapTableDependency(SignalServer signalServer)
        {
            this.signalServer = signalServer;
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
                signalServer.SendTongHopNhap();
        }
    }
}
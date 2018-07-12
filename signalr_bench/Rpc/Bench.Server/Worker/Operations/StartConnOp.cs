using Bench.Common;
using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class StartConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;
        private int _errConnCnt = 0;
        public void Do(WorkerToolkit tk)
        {
            _tk = tk;
            //tk.Test.Add(1);
            Start(tk.Connections);
        }

        private void Start(List<HubConnection> connections)
        {
            Util.Log($"start connections");
            _tk.State = Stat.Types.State.HubconnConnecting;
            //var tasks = new List<Task>(connections.Count);


            var swConn = new Stopwatch();
            swConn.Start();
            int concurrency = 50;
            var tasks = new List<Task>(connections.Count);
            var i = 0;
            foreach (var conn in connections)
            {
                try
                {
                    conn.StartAsync().Wait();
                    tasks.Add(Task.Run(() => conn.StartAsync().Wait()));
                }
                catch (Exception ex)
                {
                    Util.Log($"start connection exception: {ex}");
                    _errConnCnt++;
                }

                if (i > 0 && i % concurrency == 0)
                {
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
            }
            Task.WhenAll(tasks).Wait();
            swConn.Stop();
            Util.Log($"connction time: {swConn.Elapsed.TotalSeconds}, err conn: {_errConnCnt}");

            _tk.State = Stat.Types.State.HubconnConnected;
        }
    }
}

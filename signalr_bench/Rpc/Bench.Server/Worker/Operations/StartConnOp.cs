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
        private int errConnCnt = 0;
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

            int i = 0;

            var swConn = new Stopwatch();
            swConn.Start();
            foreach (var conn in connections)
            {
                try
                {
                    i += 1;
                    int ind = i;
                    conn.StartAsync().Wait();
                }
                catch (Exception ex)
                {
                    Util.Log($"start connection exception: {ex}");
                    errConnCnt++;
                }
            }
            swConn.Stop();
            Util.Log($"connction time: {swConn.Elapsed.TotalSeconds}, err conn: {errConnCnt}");

            _tk.State = Stat.Types.State.HubconnConnected;
        }
    }
}

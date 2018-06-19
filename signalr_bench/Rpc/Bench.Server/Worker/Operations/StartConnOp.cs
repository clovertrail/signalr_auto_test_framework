﻿using Bench.Common;
using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bench.Server.Worker.Operations
{
    class StartConnOp : BaseOp, IOperation
    {
        private WorkerToolkit _tk;

        public void Do(WorkerToolkit tk)
        {
            _tk = tk;
            tk.Test.Add(1);
            Start(tk.Connections);
        }

        private void Start(List<HubConnection> connections)
        {
            Util.Log($"start connections");
            _tk.State = Stat.Types.State.HubconnConnecting;
            var tasks = new List<Task>(connections.Count);

            int i = 0;
            foreach (var conn in connections)
            {
                i += 1;
                int ind = i;
                tasks.Add(Task.Delay(10 * ind).ContinueWith(_ => conn.StartAsync()));
                //conn.StartAsync();
            }
            Task.WhenAll(tasks).Wait();
            _tk.State = Stat.Types.State.HubconnConnected;
        }
    }
}

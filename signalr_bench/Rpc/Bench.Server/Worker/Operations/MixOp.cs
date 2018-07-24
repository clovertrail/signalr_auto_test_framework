using Bench.Common;
using Bench.RpcSlave.Worker.Savers;
using Bench.RpcSlave.Worker.StartTimeOffsetGenerator;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bench.RpcSlave.Worker.Operations
{
    class MixOp: BaseOp
    {
        private IStartTimeOffsetGenerator StartTimeOffsetGenerator;
        private List<int> _sentMessages;
        private WorkerToolkit _tk;

        public void Do(WorkerToolkit tk)
        {
            

            // var waitTime = 5 * 1000;
            // Console.WriteLine($"wait time: {waitTime / 1000}s");
            // Task.Delay(waitTime).Wait();

            // _tk = tk;
            // _tk.State = Stat.Types.State.SendReady;

            // // setup
            // Setup();
            // Task.Delay(5000).Wait();

            // _tk.State = Stat.Types.State.SendRunning;
            // Task.Delay(5000).Wait();

            // // send message
            // StartSendMsg();

            // Task.Delay(30 * 1000).Wait();

            // // save counters
            // SaveCounters();

            // _tk.State = Stat.Types.State.SendComplete;
            // Util.Log($"Sending Complete");
        }

        
    }
}

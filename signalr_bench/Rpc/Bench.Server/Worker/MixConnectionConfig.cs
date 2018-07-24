using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;

namespace Bench.RpcSlave.Worker
{
    public class MixConnectionConfig
    {
        public (int, int) EchoRange;
        public (int, int) BroadcastRange;
        public (int, int) GroupRange;

        public void Allocate(int echoConnCnt, int broadcastConnCnt, int groupConnCnt)
        {
            
        }
    }
}
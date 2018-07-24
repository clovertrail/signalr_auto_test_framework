using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;

namespace Bench.RpcSlave.Worker
{
    public class MixConnectionConfig
    {
        public (int, int) EchoRange = (-1,-1);
        public (int, int) BroadcastRange = (-1,-1);
        public (int, int) GroupRange = (-1,-1);

        public void Allocate(int totalConnCnt, int echoConnCnt, int broadcastConnCnt, int groupConnCnt)
        {
            if (totalConnCnt != echoConnCnt + broadcastConnCnt + groupConnCnt)
            {
                Util.Log("Wrong number for MixConnectionConfig");
                throw new Exception();
            }
            else
            {
                int startInd = 0;
                EchoRange = (startInd, startInd + echoConnCnt);
                startInd += echoConnCnt;
                BroadcastRange = (startInd, startInd + broadcastConnCnt);
                startInd += broadcastConnCnt;
                GroupRange = (startInd, startInd + groupConnCnt);
            }
        }
    }
}
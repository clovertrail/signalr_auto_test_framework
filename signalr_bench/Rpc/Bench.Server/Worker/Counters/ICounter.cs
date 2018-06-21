using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;



namespace Bench.RpcSlave.Worker.Counters
{
    public interface ICounters
    {
        int LatencyStep { get; set; }
        int LatencyLength { get; set; }
        List<Tuple<string, int>> GetAll();
        void ResetCounters();
        void CountLatency(long sendTimestamp, long receiveTimestamp);
        void IncreseSentMsg();
        void SaveCounters();
    }
}

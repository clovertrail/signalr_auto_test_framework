using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bench.Common;
using Bench.Server.Worker.Savers;

namespace Bench.Server.Worker.Counters
{
    public class Counter: ICounters
    {
        private ConcurrentDictionary<string, int> InnerCounters { get; set; }
        public int LatencyStep { get; set; }
        public int LatencyLength { get; set; }
        private ISaver _counterSaver;


        public Counter(ISaver saver, int latencyStep=100, int latencyLength=10)
        {
            LatencyStep = latencyStep;
            LatencyLength = latencyLength;
            _counterSaver = saver;
            InnerCounters = new ConcurrentDictionary<string, int>();
            ResetCounters();
        }

        public List<Tuple<string, int>> GetAll()
        {
            var list = new List<Tuple<string, int>>();
            lock (InnerCounters)
            {
                foreach (var counter in InnerCounters)
                {
                    list.Add(new Tuple<string, int>(counter.Key, counter.Value));
                }
            }
            
            return list;
        }

        public void ResetCounters()
        {
            for (int i = 1; i <= LatencyLength; i++)
            {
                InnerCounters.AddOrUpdate(MsgKey(i * LatencyStep), 0, (k, v) => 0);
            }
            InnerCounters.AddOrUpdate("message:sent", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate($"message:ge:{LatencyLength * LatencyStep}", 0, (k, v) => 0);
        }

        public void CountLatency(long sendTimestamp, long receiveTimestamp)
        {
            long dTime = receiveTimestamp - sendTimestamp;
            for (int j = 1; j <= LatencyLength; j++)
            {
                if (dTime < j * LatencyStep)
                {
                    InnerCounters.AddOrUpdate(MsgKey(j * LatencyStep), 0, (k, v) => v + 1);
                    return;
                }
            }

            InnerCounters.AddOrUpdate($"message:ge:{LatencyLength * LatencyStep}", 0, (k, v) => v + 1);
        }

        public void IncreseSentMsg()
        {
            InnerCounters.AddOrUpdate("message:sent", 0, (k, v) => v + 1);
        }

        private string MsgKey(int latency)
        {
            return $"message:lt:{latency}";
        }

        public void SaveCounters()
        {
            // TODO: choose lightest lock
            lock (InnerCounters)
            {
                _counterSaver.Save("Record.txt", Util.Timestamp(), InnerCounters);
            }
        }
    }

    
}

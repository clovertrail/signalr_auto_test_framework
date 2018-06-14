using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Bench.Server.Worker.Savers
{
    public interface ISaver
    {
        void Save(string url, long timestamp, ConcurrentDictionary<string, int> counters);
    }
}

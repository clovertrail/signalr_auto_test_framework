using Bench.Common;
using Bench.Common.Config;
using Bench.Server.Worker.Counters;
using Bench.Server.Worker.Savers;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;

namespace Bench.Server.Worker
{
    public class WorkerToolkit
    {
        public JobConfig JobConfig { get; set; }
        public List<HubConnection> Connections { get; set; }
        public Stat.Types.State State { get; set; } = Stat.Types.State.WorkerUnexist;
        public List<int> Test { get; set; }
        public ICounters Counters { get; set; } = new Counter(new LocalFileSaver());

    }
}
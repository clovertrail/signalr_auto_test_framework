using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Bench.Server.Worker.Operations;
using Bench.Common;

namespace Bench.Server.Worker
{
    class SigWorker
    {
        private WorkerToolkit _tk = new WorkerToolkit();

        public void LoadJobs(JobConfig jobConfig)
        {
            _tk.JobConfig = jobConfig; 
        }

        public Stat.Types.State ProcessJob()
        {
            // process operations
            GetPipeline().ForEach(opName =>
            {
                var tuple = OperationFactory.CreateOp(opName);
                var obj = tuple.Item1;
                var type = tuple.Item2;
                dynamic op = Convert.ChangeType(obj, type);
                op.Do(_tk);
            });

            return Stat.Types.State.SendComplete;
        }

        public List<string> GetPipeline()
        {
            return _tk.JobConfig.Pipeline;
        }

        public List<Tuple<string, int>> GetCounters()
        {
            return _tk.Counters.GetAll();
        }

        public void UpdateState(Stat.Types.State state)
        {
            _tk.State = state;
        }

        public Stat.Types.State GetState()
        {
            return _tk.State;
        }
    }
}

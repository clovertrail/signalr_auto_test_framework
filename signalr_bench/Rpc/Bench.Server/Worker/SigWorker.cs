using Bench.Common.Config;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using Bench.RpcSlave.Worker.Operations;
using Bench.Common;

namespace Bench.RpcSlave.Worker
{
    class SigWorker
    {
        private WorkerToolkit _tk = new WorkerToolkit();

        public void LoadJobs(JobConfig jobConfig)
        {
            _tk.JobConfig = jobConfig; 
        }

        public void LoadBenchmarkCellConfig(BenchmarkCellConfig benchmarkCellConfig)
        {
            _tk.BenchmarkCellConfig = benchmarkCellConfig;
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

            //var op1 = new CreateConnOp();
            //var op2 = new StartConnOp();
            //var op3 = new EchoOp();
            //var op4 = new StopConnOp();
            //var op5 = new DisposeConnOp();

            //op1.Do(_tk);
            //op2.Do(_tk);
            //op3.Do(_tk);
            //op4.Do(_tk);
            //op5.Do(_tk);

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

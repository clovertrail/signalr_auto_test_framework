using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bench.RpcSlave.Worker.Operations;

namespace Bench.RpcSlave
{
    public class RpcServiceImpl : RpcService.RpcServiceBase
    {
        SigWorker _sigWorker;

        public override Task<Timestamp> GetTimestamp(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Timestamp { Time = 123 });
        }

        public override Task<Stat> GetState(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Stat { State = _sigWorker.GetState() });
        }

        public override Task<Strg> GetCounterJsonStr(Empty empty, ServerCallContext context)
        {
            return Task.FromResult(new Strg { Str = "json string" });
        }

        public override Task<Stat> LoadJobConfig(CellJobConfig config, ServerCallContext context)
        {
            //// load job config
            //var jobConfigLoader = new ConfigLoader();
            //var jobConfig = jobConfigLoader.Load<JobConfig>(path.Ppath);
            var jobConfig = new JobConfig
            {
                Connections = config.Connections,
                Slaves = config.Slaves,
                Interval = config.Interval,
                Duration = config.Duration,
                ServerUrl = config.ServerUrl,
                Pipeline = new List<string>(config.Pipline.Split(';'))
            };

            // TODO: handle exception
            if (_sigWorker == null)
            {
                _sigWorker = new SigWorker();
            }

            _sigWorker.LoadJobs(jobConfig);
            _sigWorker.UpdateState(Stat.Types.State.ConfigLoaded);
            return Task.FromResult(new Stat { State = Stat.Types.State.ConfigLoaded});
        }

        public override Task<Stat> CreateWorker(Empty empty, ServerCallContext context)
        {
            if (_sigWorker != null)
            {
                _sigWorker.UpdateState(Stat.Types.State.WorkerExisted);   
                return Task.FromResult(new Stat { State = Stat.Types.State.WorkerExisted });
            }

            _sigWorker = new SigWorker();
            _sigWorker.UpdateState(Stat.Types.State.WorkerCreated);   
            return Task.FromResult(new Stat { State = Stat.Types.State.WorkerCreated });
        }

        public override Task<CounterDict> CollectCounters(Force force, ServerCallContext context)
        {
            var dict = new CounterDict();
            if (force.Force_ != true && (int)_sigWorker.GetState() < (int)Stat.Types.State.SendRunning)
            {
                return Task.FromResult(dict);
            }

            var list = _sigWorker.GetCounters();
            list.ForEach(pair => dict.Pairs.Add(new Pair { Key = pair.Item1, Value = pair.Item2 }));
            return Task.FromResult(dict);
        }

        public override Task<Stat> RunJob(Common.BenchmarkCellConfig cellConfig, ServerCallContext context)
        {
            Console.WriteLine($"Run Job");
            Worker.BenchmarkCellConfig benchmarkCellConfig = new Worker.BenchmarkCellConfig
            {
                ServiceType = cellConfig.ServiveType,
                HubProtocol = cellConfig.HubProtocol,
                TransportType = cellConfig.TransportType,
                Scenario = cellConfig.Scenario
            };
            _sigWorker.LoadBenchmarkCellConfig(benchmarkCellConfig);
            _sigWorker.ProcessJob();

            return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo });

        }

        public override Task<Stat> Test(Strg strg, ServerCallContext context)
        {
            return Task.FromResult(new Stat { State = Stat.Types.State.DebugTodo});
        }

    }
}

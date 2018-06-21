// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Grpc.Core;
using Bench.Common;
using Bench.Common.Config;
using CommandLine;
using System.Collections.Generic;
using Bench.RpcMaster.Allocators;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bench.RpcMaster
{
    class Program
    {
        public static void Main (string[] args)
        {
            // parse args
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });

            // load agents config
            var agentConfigLoader = new ConfigLoader();
            var agentConfig = agentConfigLoader.Load<AgentConfig>(argsOption.AgentConfigFile);

            agentConfig.Slaves.ForEach(slv => Util.Log($"slave: {slv}"));

            // open channel to rpc servers
            var channels = new List<Channel>(agentConfig.Slaves.Count);
            for (var i = 0; i < agentConfig.Slaves.Count; i++)
            {
                Util.Log($"add channel: {agentConfig.Slaves[i]}:{agentConfig.RpcPort}");
                channels.Add(new Channel($"{agentConfig.Slaves[i]}:{agentConfig.RpcPort}", ChannelCredentials.Insecure));
            }

            // create rpc clients
            var clients = new List<RpcService.RpcServiceClient>(agentConfig.Slaves.Count);
            for (var i = 0; i < agentConfig.Slaves.Count; i++)
            {
                clients.Add(new RpcService.RpcServiceClient(channels[i]));
            }

            // load job config
            var jobConfigLoader = new ConfigLoader();
            var jobConfig = jobConfigLoader.Load<JobConfig>(argsOption.JobConfigFile);

            // allocate connections/protocol/transport type...
            // TODO, only for dev
            //var criteria = new Dictionary<string, int>();
            //var allocator = new OneAllocator();
            //var allocated = allocator.Allocate(agentConfig.Slaves, jobConfig.Connections, criteria);

            // call salves to load job config
            clients.ForEach( client =>
            {
                var state = new Stat();
                state = client.CreateWorker(new Empty());
                Util.Log($"create worker state: {state.State}");
                state = client.LoadJobConfig(new Common.Path { Ppath = argsOption.JobConfigFile });
                Util.Log($"load job config state: {state.State}");
            });

            // collect counters
            var collectTimer = new System.Timers.Timer(1000);
            collectTimer.AutoReset = true;
            collectTimer.Elapsed += (sender, e) =>
            {
                var allClientCounters = new ConcurrentDictionary<string, int>();
                clients.ForEach(client =>
                {
                    var state = client.GetState(new Empty { });
                    if ((int)state.State < (int)Stat.Types.State.SendRunning) return;
                    var counters = client.CollectCounters(new Force { Force_ = false });

                    for (var i = 0; i < counters.Pairs.Count; i++)
                    {
                        var key = counters.Pairs[i].Key;
                        var value = counters.Pairs[i].Value;
                        allClientCounters.AddOrUpdate(key, value, (k, v) => v + value);
                    }
                });

                var jobj = new JObject();
                var received = 0;
                foreach (var item in allClientCounters)
                {
                    jobj.Add(item.Key, item.Value);
                    if (!item.Key.Contains("sent"))
                    {
                        received += item.Value;
                    }
                }

                jobj.Add("message:received", received);
                var sortedCounters = Util.Sort(jobj);
                var finalRec = new JObject
                {
                    { "Time", Util.Timestamp2DateTimeStr(Util.Timestamp()) },
                    { "Counters", sortedCounters}
                };
                string oneLineRecord = Regex.Replace(finalRec.ToString(), @"\s+", "");
                oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "");
                oneLineRecord += "," + Environment.NewLine;

                var dir = System.IO.Path.GetDirectoryName(argsOption.OutputCounterFile);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!File.Exists(argsOption.OutputCounterFile))
                {
                    StreamWriter sw = File.CreateText(argsOption.OutputCounterFile);
                }

                File.AppendAllText(argsOption.OutputCounterFile, oneLineRecord);
                Util.Log("per second: " + oneLineRecord);

            };
            collectTimer.Start();


            // process pipeline
            var tasks = new List<Task>();
            var benchmarkCellConfig = new BenchmarkCellConfig
            {
                ServiveType = argsOption.ServiceType,
                TransportType = argsOption.TransportType,
                HubProtocol = argsOption.HubProtocal,
                Scenario = argsOption.Scenario
            };
            clients.ForEach(client => tasks.Add(Task.Delay(0).ContinueWith(t => Task.FromResult(client.RunJob(benchmarkCellConfig)))));

            Util.Log($"wait for tasks");
            Task.WhenAll(tasks).Wait();

            for (var i = 0; i < channels.Count; i++)
            {
                channels[i].ShutdownAsync().Wait();
            }

            Console.WriteLine ("Exit client...");
        }
    }
}
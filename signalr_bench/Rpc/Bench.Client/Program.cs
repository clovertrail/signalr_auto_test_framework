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
using Bench.Client.Allocators;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Concurrent;

namespace Bench.Client
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
                int perclient = 0;
                int ind = 0;
                Util.Log($"clients cnt: {clients.Count}");
                clients.ForEach(client =>
                {
                    var state = client.GetState(new Empty { });
                    Util.Log($"{ind}th client:");
                    ind = ind + 1;
                    if ((int)state.State < (int)Stat.Types.State.SendRunning) return;
                    var counters = client.CollectCounters(new Force { Force_ = false });

                    for (var i = 0; i < counters.Pairs.Count; i++)
                    {
                        var key = counters.Pairs[i].Key;
                        var value = counters.Pairs[i].Value;
                        allClientCounters.AddOrUpdate(key, value, (k, v) => v + value);
                        perclient += value;
                        Util.Log($"{key}: {value}");
                    }
                });

                var jobj = new JObject();
                foreach(var item in allClientCounters)
                {
                    jobj.Add(item.Key, item.Value);
                }

                var sortedCounters = Util.Sort(jobj);
                string oneLineRecord = Regex.Replace(sortedCounters.ToString(), @"\s+", "");
                oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "") + Environment.NewLine;
                oneLineRecord = $"[{Util.Timestamp2DateTimeStr(Util.Timestamp())}]: {oneLineRecord}";
                oneLineRecord += Convert.ToString(perclient);

                if (!File.Exists("PerSecond.txt"))
                {
                    StreamWriter sw = File.CreateText("PerSecond.txt");
                }

                File.AppendAllText("PerSecond.txt", oneLineRecord);
                Util.Log("per second: " + oneLineRecord);

            };
            collectTimer.Start();
            

            // process pipeline
            clients.ForEach(client => client.RunJob(new Empty()));

            for (var i = 0; i < channels.Count; i++)
            {
                channels[i].ShutdownAsync().Wait();
            }

            Console.WriteLine ("Exit client...");
        }
    }
}
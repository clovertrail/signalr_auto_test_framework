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


            var slaveList = new List<string>(argsOption.SlaveList);

            // open channel to rpc servers
            var channels = new List<Channel>(slaveList.Count);
            for (var i = 0; i < slaveList.Count; i++)
            {
                Util.Log($"add channel: {slaveList[i]}:{argsOption.RpcPort}");
                channels.Add(new Channel($"{slaveList[i]}:{argsOption.RpcPort}", ChannelCredentials.Insecure));
            }

            // create rpc clients
            var clients = new List<RpcService.RpcServiceClient>(slaveList.Count);
            for (var i = 0; i < slaveList.Count; i++)
            {
                clients.Add(new RpcService.RpcServiceClient(channels[i]));
            }

            // load job config
            var jobConfig = new JobConfig(argsOption);

            // allocate connections/protocol/transport type...
            // TODO, only for dev
            //var criteria = new Dictionary<string, int>();
            //var allocator = new OneAllocator();
            //var allocated = allocator.Allocate(slaveList, jobConfig.Connections, criteria);

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
                var collectCountersTasks = new List<Task>();
                var ind = 0;
                var isSend = false;
                var isComplete = false;
                clients.ForEach(client =>
                {
                    collectCountersTasks.Add(
                        Task.Delay(0).ContinueWith(t =>
                            {
                                var state = client.GetState(new Empty { });
                                if ((int)state.State >= (int)Stat.Types.State.SendComplete || (int)state.State < (int)Stat.Types.State.SendRunning)
                                {
                                    isComplete = true;
                                    return;
                                }
                                if ((int)state.State < (int)Stat.Types.State.SendRunning) return;
                                isSend = true;
                                Util.Log($"ind: {ind++}, state: {state.State}");
                                var counters = client.CollectCounters(new Force { Force_ = false });

                                for (var i = 0; i < counters.Pairs.Count; i++)
                                {
                                    var key = counters.Pairs[i].Key;
                                    var value = counters.Pairs[i].Value;
                                    allClientCounters.AddOrUpdate(key, value, (k, v) => v + value);
                                }
                            }
                        )
                    );
                    
                });

                Task.WhenAll(collectCountersTasks).Wait();

                if (isSend == false || isComplete == true)
                {
                    return;
                }

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
            var tasks = new List<Task>(clients.Count);
            var benchmarkCellConfig = new BenchmarkCellConfig
            {
                ServiveType = argsOption.ServiceType,
                TransportType = argsOption.TransportType,
                HubProtocol = argsOption.HubProtocal,
                Scenario = argsOption.Scenario
            };
            Util.Log($"service: {benchmarkCellConfig.ServiveType}; transport: {benchmarkCellConfig.TransportType}; hubprotocol: {benchmarkCellConfig.HubProtocol}; scenario: {benchmarkCellConfig.Scenario}");

            var indStartJob = 0;
            //clients.ForEach(client => {
            //        Util.Log($"client add task ind: {indStartJob}");
            //        tasks.Add(
            //            Task.Delay(0).ContinueWith(
            //                t =>
            //                {
            //                    Util.Log($"client start ind: {indStartJob++}");
            //                    client.RunJob(benchmarkCellConfig);
            //                }
            //            )
            //        );
            //    }
            //);

            clients.ForEach(client =>
            {
                Util.Log($"client add task ind: {indStartJob}");
                tasks.Add(Task.Run(() =>
                {
                    Util.Log($"client start ind: {indStartJob++}");
                    client.RunJob(benchmarkCellConfig);
                }));
            });

            //Action[] actions = new Action[clients.Count];
            //for (var i = 0; i < clients.Count; i++)
            //{
            //    int ind = i;
            //    actions[i] = () => 
            //    {
            //        Util.Log($"client start");
            //        clients[ind].RunJob(benchmarkCellConfig);
            //    };
            //}
            //Parallel.Invoke(actions);

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
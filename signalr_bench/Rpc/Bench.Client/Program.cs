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
        private static JObject _counters;
        private static string _jobResultFile = "./jobResult.txt";
        public static void Main (string[] args)
        {
            // parse args
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });

            if (argsOption.Clear == "true")
            {
                if (File.Exists(_jobResultFile))
                {
                    File.Delete(_jobResultFile);
                }
            } 
            else
            {
                if (File.Exists(_jobResultFile))
                {
                    CheckLastJobResults(_jobResultFile, argsOption.Retry, argsOption.Connections,
                        argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                }
                
            }

            var slaveList = new List<string>(argsOption.SlaveList.Split(';'));

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
               var config = new CellJobConfig 
               {
                    Connections = argsOption.Connections,
                    Slaves = argsOption.Slaves,
                    Interval = argsOption.Interval,
                    Duration = argsOption.Duration,
                    ServerUrl = argsOption.ServerUrl,
                    Pipeline = argsOption.PipeLine
               };
               Util.Log($"create worker state: {state.State}");
               state = client.LoadJobConfig(config);
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
                Util.Log($"all clients");
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
                                var counters = client.CollectCounters(new Force { Force_ = false });

                                for (var i = 0; i < counters.Pairs.Count; i++)
                                {
                                    var key = counters.Pairs[i].Key;
                                    var value = counters.Pairs[i].Value;
                                    if (key.Contains("server"))
                                    {
                                        Util.Log($"server [{key}]:[{value}]");
                                    }
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
                    if (item.Key.Contains("message") && (item.Key.Contains(":ge") || item.Key.Contains(":lt")))
                    {
                        received += item.Value;
                    }
                }

                jobj.Add("message:received", received);
                _counters = Util.Sort(jobj);
                var finalRec = new JObject
                {
                    { "Time", Util.Timestamp2DateTimeStr(Util.Timestamp()) },
                    { "Counters", _counters}
                };
                string onelineRecord = Regex.Replace(finalRec.ToString(), @"\s+", "");
                onelineRecord = Regex.Replace(onelineRecord, @"\t|\n|\r", "");
                onelineRecord += "," + Environment.NewLine;
                Util.Log("per second: " + onelineRecord);

                try
                {
                    var dir = System.IO.Path.GetDirectoryName(argsOption.OutputCounterFile);
                    if (!Directory.Exists(dir))
                    {
                        if (dir != null && dir != "")
                        {
                            Directory.CreateDirectory(dir);
                        }
                    }
                    if (!File.Exists(argsOption.OutputCounterFile))
                    {
                        StreamWriter sw = File.CreateText(argsOption.OutputCounterFile);
                    }

                    File.AppendAllText(argsOption.OutputCounterFile, onelineRecord);
                }
                catch (Exception ex)
                {
                    Util.Log($"Cannot save file: {ex}");
                }
                

                


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

            try
            {
                var indStartJob = 0;
                clients.ForEach(client =>
                {
                    Util.Log($"client add task ind: {indStartJob}");
                    tasks.Add(Task.Run(() =>
                    {
                        Util.Log($"client start ind: {indStartJob++}");
                        client.RunJob(benchmarkCellConfig);
                    }));
                });
                Util.Log($"wait for tasks");
                Task.WhenAll(tasks).Wait();
                SaveJobResult(_jobResultFile, _counters, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);

            }
            catch (Exception ex)
            {
                Util.Log($"Exception from RPC master: {ex}");
                var counters = new JObject(_counters);
                counters["message:received"] = 0;
                SaveJobResult(_jobResultFile, counters, argsOption.Connections, argsOption.ServiceType, argsOption.TransportType, argsOption.HubProtocal, argsOption.Scenario);
                throw;
            }
            
            

            for (var i = 0; i < channels.Count; i++)
            {
                channels[i].ShutdownAsync().Wait();
            }

            Console.WriteLine ("Exit client...");
        }

        private static void SaveJobResult(string path, JObject counters, int connection, string serviceType, string transportType, string protocol, string scenario)
        {
            var resDir = System.IO.Path.GetDirectoryName(path);
            if (!Directory.Exists(resDir))
            {
                Directory.CreateDirectory(resDir);
            }
            if (!File.Exists(path))
            {
                StreamWriter sw = File.CreateText(path);
            }

            var sent = (int)counters["message:sent"];
            var notSent = (int)counters["message:notSentFromClient"];
            var total = sent + notSent;
            var received = (int)counters["message:received"];
            var percentage = 0.0;
            if (scenario.Contains("broadcast"))
            {
                percentage = (double)received / (total * connection);
            }
            else
            {
                percentage = (double)received / (total);
            }
            var result = percentage > 0.8 ? "SUCCESS" : "FAIL";

            Util.Log($"sent: {sent}, received: {received}");
            
            var res = new JObject
            {
                { "connection", connection},
                { "serviceType", serviceType},
                { "transportType", transportType},
                { "protocol", protocol},
                { "scenario", scenario},
                {"result",  result}
            };

            string onelineRecord = Regex.Replace(res.ToString(), @"\s+", "");
            onelineRecord = Regex.Replace(onelineRecord, @"\t|\n|\r", "");
            onelineRecord += Environment.NewLine;
            File.AppendAllText(path, onelineRecord);

            if (result == "FAIL")
            {
                throw new Exception();
            }
        }

        private static void CheckLastJobResults(string path, int maxRetryCount, int connection, string serviceType, 
            string transportType, string protocol, string scenario)
        {
            var failCount = 0;
            var lines = new List<string>(File.ReadAllLines(path));
            for (var i = lines.Count - 1; i > lines.Count - 1 - maxRetryCount - 1  && i >= 0; i--)
            {
                JObject res = null;
                try
                {
                    res = JObject.Parse(lines[i]);
                }
                catch (Exception ex)
                {
                    Util.Log($"parse result: {lines[i]}\n Exception: {ex}");
                    continue;
                }
                if ((string)res["serviceType"] == serviceType &&
                    (string)res["transportType"] == transportType && (string)res["protocol"] == protocol &&
                    (string)res["scenario"] == scenario && (string)res["result"] == "FAIL")
                {
                    failCount++;
                }
                else
                {
                    break;
                }
            }
            Util.Log($"fail count: {failCount}");
            if (failCount >= maxRetryCount)
            {
                Util.Log("Too many fails. Break job");
                throw new Exception();
            }
            
        }
    }
}
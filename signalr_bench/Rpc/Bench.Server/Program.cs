using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Bench.Common;
using CommandLine;
using Bench.Common.Config;
using Bench.Server.Worker;

namespace Bench.Server {
    

    class Program
    {
        public static void Main (string[] args)
        {
            Console.WriteLine("MachineName: {0}", Environment.MachineName);

            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });

            var configLoader = new ConfigLoader();
            var config = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);


            /* debug */
            //var jobConfigLoader = new ConfigLoader();
            //var jobConfig = jobConfigLoader.Load<JobConfig>(argsOption.AgentConfigFile);

            //var jobConfig = new JobConfig();
            //jobConfig.Duration = 10;
            //jobConfig.Interval = 1;
            //jobConfig.Connections = 500;
            //jobConfig.Slaves = 1;
            //jobConfig.HubProtocol = "json";
            //jobConfig.TransportType = "websockets";
            //jobConfig.ServerUrl = "http://wanlsignalrautotestappserver.eastus.cloudapp.azure.com:5000/echo";
            //jobConfig.CallbackName = "EchoCallback";

            //var _sigWorker = new SigWorker();
            //_sigWorker.LoadJobs(jobConfig);
            //_sigWorker.ProcessJob();



            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { RpcService.BindService(new RpcServiceImpl()) },
                Ports = { new ServerPort(argsOption.DnsName, config.RpcPort, ServerCredentials.Insecure) }
            };
            server.Start();

            //Console.WriteLine ("Server listening on port " + config.RpcPort);
            //Console.WriteLine ("Press any key to stop the server...");
            //Console.ReadKey ();

            Task.Delay(Timeout.Infinite).Wait();

            server.ShutdownAsync().Wait();
        }
    }
}
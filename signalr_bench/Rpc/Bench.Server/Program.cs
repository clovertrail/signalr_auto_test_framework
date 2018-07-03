using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Bench.Common;
using CommandLine;
using Bench.Common.Config;
using Bench.RpcSlave.Worker;

namespace Bench.RpcSlave {
    

    class Program
    {
        public static void Main (string[] args)
        {
            Console.WriteLine("MachineName: {0}", Environment.MachineName);

            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });


            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { RpcService.BindService(new RpcServiceImpl()) },
                Ports = { new ServerPort(argsOption.DnsName, argsOption.RpcPort, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine($"Server [{argsOption.DnsName}:{argsOption.RpcPort}] started");
            Task.Delay(Timeout.Infinite).Wait();
            
        }
    }
}
using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JenkinsScript
{
    class Program
    {


        static void Main(string[] args)
        {
            // read options
            var argsOption = new ArgsOption();
            _ = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error => { });

            // parse agent config file
            var configLoader = new ConfigLoader();
            var cfg = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);
            Util.Log("finish loading config");

            var hosts = new List<string>();
            hosts.Add(cfg.AppServer);
            cfg.Slaves.ForEach(slv => hosts.Add(slv));
            hosts.Add(cfg.Master);

            var errCode = 0;
            var result = "";
            var cmd = "";

            // git clone repo
            hosts.ForEach(host =>
            {
                //cmd = $"rm -rf /home/{cfg.User}/signalr_auto_test_framework; cp -rf /home/{cfg.User}/workspace/signalr_auto_test_framework /home/{cfg.User}/signalr_auto_test_framework"; //TODO
                cmd = $"rm -rf /home/{cfg.User}/signalr_auto_test_framework; git clone {cfg.Repo} /home/{cfg.User}/signalr_auto_test_framework"; //TODO
                Util.Log($"CMD: {cfg.User}@{host}: {cmd}");
                if (host == cfg.Master) { }
                else (errCode, result) = ShellHelper.RemoteBash(cfg.User, host, cfg.SshPort, cfg.Password, cmd);
                if (errCode != 0) return;
                return;//TODO: only for debug, to remove
            });

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            // kill all dotnet process
            hosts.ForEach(host =>
            {
                cmd = $"killall dotnet || true";
                if (host.Contains("localhost") || host.Contains("127.0.0.1")) { }
                else if (host == cfg.Master)
                {
                    Util.Log($"CMD: {cfg.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash(cfg.User, host, cfg.SshPort, cfg.Password, cmd);
                }
                else
                {
                    Util.Log($"CMD: {cfg.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash(cfg.User, host, cfg.SshPort, cfg.Password, cmd);
                }
                if (errCode != 0) return;
            });

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            // start appserver
            //cmd = $"cd /home/{cfg.User}/workspace/signalr_auto_test_framework/signalr_bench/AppServer/; dotnet run > log.txt";
            cmd = $"cd /home/{cfg.User}/signalr_auto_test_framework/signalr_bench/AppServer/;export AzureSignalRConnectionString='{argsOption.AzureSignalrConnectionString}'; dotnet run > log.txt";
            Util.Log($"{cfg.User}@{cfg.AppServer}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(cfg.User, cfg.AppServer, cfg.SshPort, cfg.Password, cmd, wait: false);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            Task.Delay(5000).Wait();

            // start rpc agents
            cfg.Slaves.ForEach(host =>
            {
                //cmd = $"cd /home/{cfg.User}/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Server/; dotnet run -a {argsOption.AgentConfigFile} -d 0.0.0.0 > log.txt";
                cmd = $"cd /home/{cfg.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Server/; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -a '{argsOption.AgentConfigFile}' -d 0.0.0.0 > log.txt";
                Util.Log($"CMD: {cfg.User}@{host}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(cfg.User, host, cfg.SshPort, cfg.Password, cmd, wait: false);
                if (errCode != 0) return;
            });
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            Task.Delay(20000).Wait();

            // start master
            //cmd = $"cd /home/{cfg.User}/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; dotnet run -a {argsOption.AgentConfigFile} -j {argsOption.JobConfigFile} > log.txt";
            cmd = $"cd /home/{cfg.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -a '{argsOption.AgentConfigFile}' -j '{argsOption.JobConfigFile}' > log.txt";
            Util.Log($"CMD: {cfg.User}@{cfg.Master}: {cmd}");
            var maxRetry = 100;
            for (var i = 0; i < maxRetry; i++)
            {
                (errCode, result) = ShellHelper.RemoteBash(cfg.User, cfg.Master, cfg.SshPort, cfg.Password, cmd);
                if (errCode == 0) break;
                Util.Log($"retry {i}th time");
                Util.Log($"CMD: {cfg.User}@{cfg.Master}: {cmd}");
                Task.Delay(2000).Wait();

                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                }
            }
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }


        }
    }
}

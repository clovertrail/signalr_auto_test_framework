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
            var agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);
            var jobConfig = configLoader.Load<JobConfig>(argsOption.JobConfigFile);
            Util.Log("finish loading config");

            var hosts = new List<string>();
            hosts.Add(agentConfig.AppServer);
            agentConfig.Slaves.ForEach(slv => hosts.Add(slv));
            hosts.Add(agentConfig.Master);

            var errCode = 0;
            var result = "";
            var cmd = "";

            // git clone repo
            hosts.ForEach(host =>
            {
                cmd = $"rm -rf /home/{agentConfig.User}/signalr_auto_test_framework; git clone {agentConfig.Repo} /home/{agentConfig.User}/signalr_auto_test_framework"; //TODO
                Util.Log($"CMD: {agentConfig.User}@{host}: {cmd}");
                if (host == agentConfig.Master) { }
                else (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
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
                else if (host == agentConfig.Master)
                {
                    Util.Log($"CMD: {agentConfig.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
                }
                else
                {
                    Util.Log($"CMD: {agentConfig.User}@{host}: {cmd}");
                    (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
                }
                if (errCode != 0) return;
            });

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            // start appserver
            cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/AppServer/; export AzureSignalRConnectionString='{argsOption.AzureSignalrConnectionString}'; dotnet run > log.txt";
            Util.Log($"{agentConfig.User}@{agentConfig.AppServer}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.AppServer, agentConfig.SshPort, agentConfig.Password, cmd, wait: false);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            Task.Delay(5000).Wait();

            // start rpc agents
            agentConfig.Slaves.ForEach(host =>
            {
                cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Server/; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -a '{argsOption.AgentConfigFile}' -d 0.0.0.0 > log.txt";
                Util.Log($"CMD: {agentConfig.User}@{host}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd, wait: false);
                if (errCode != 0) return;
            });
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            Task.Delay(20000).Wait();

            // start master
            cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; export bench_type_list='service'; export bench_codec_list='{jobConfig.HubProtocol}'; bench_name_list='{jobConfig.Pipeline[2]}'; export result_root=`date +%Y%m%d%H%M%S`; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -a '{argsOption.AgentConfigFile}' -j '{argsOption.JobConfigFile}' -o '/home/{agentConfig.User}/signalr-bench/{Environment.GetEnvironmentVariable("result_root")}/{Environment.GetEnvironmentVariable("bench_type_list")}_{Environment.GetEnvironmentVariable("bench_codec_list")}_{Environment.GetEnvironmentVariable("bench_name_list")}/counters.txt'";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            var maxRetry = 100;
            for (var i = 0; i < maxRetry; i++)
            {
                (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);
                if (errCode == 0) break;
                Util.Log($"retry {i}th time");
                Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
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

            // gen report
            cmd = $"cd /home/{agentConfig.User}/signalr-bench/; sh gen_html.sh; sh gen_all_report.sh; sh publish_report.sh; sh gen_summary.sh;";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            var startInd = argsOption.OutputCounterFile.IndexOf("signalr-bench/") + "signalr-bench/".Length;
            Util.Log($"Report: http://wanlsignalrbenchserver.eastus.cloudapp.azure.com:8000/" + argsOption.OutputCounterFile.Substring(startInd));
        }
    }
}

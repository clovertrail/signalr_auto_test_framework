using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace JenkinsScript
{
    class ShellHelper
    {
        public static (int, string) Bash(string cmd, bool wait=true)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            var result = "";
            var errCode = 0;
            if (wait == true) result = process.StandardOutput.ReadToEnd();
            if (wait == true) process.WaitForExit();
            if (wait == true) errCode = process.ExitCode;
            return (errCode, result);
        }

        public static (int, string) RemoteBash(string user, string host, int port, string password, string cmd, bool wait=true)
        {
            if (host.IndexOf("localhost") >= 0 || host.IndexOf("127.0.0.1") >= 0) return Bash(cmd, wait);
            string sshPassCmd = $"ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
            return Bash(sshPassCmd, wait);
        }

        public static (int, string) KillAllDotnetProcess(List<string> hosts, AgentConfig agentConfig)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

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

            return (errCode, result);
        }

        public static (int, string) GitCloneRepo(List<string> hosts, AgentConfig agentConfig)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

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

            return (errCode, result);
        }

        public static (int, string) StartAppServer(List<string> hosts, AgentConfig agentConfig, ArgsOption argsOption)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/AppServer/; export AzureSignalRConnectionString='{argsOption.AzureSignalrConnectionString}'; dotnet run > log.txt";
            Util.Log($"{agentConfig.User}@{agentConfig.AppServer}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.AppServer, agentConfig.SshPort, agentConfig.Password, cmd, wait: false);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);

        }

        public static (int, string) StartRpcSlaves(List<string> hosts, AgentConfig agentConfig, ArgsOption argsOption)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

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

            return (errCode, result);

        }

        public static (int, string) StartRpcMaster(List<string> hosts, AgentConfig agentConfig, ArgsOption argsOption, string serviceType, string transportType, string hubProtocol, string scenario)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var bench_type_list = serviceType;
            var bench_codec_list = hubProtocol;
            var bench_name_list = scenario;
            cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; export bench_type_list='service'; export bench_codec_list='{hubProtocol}'; export bench_name_list='{scenario}'; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -- -v {serviceType} -t {transportType} -p {hubProtocol} -s {scenario} -a '{argsOption.AgentConfigFile}' -j '{argsOption.JobConfigFile}' -o '/home/{agentConfig.User}/signalr-bench/{Environment.GetEnvironmentVariable("result_root")}/{bench_type_list}_{bench_codec_list}_{bench_name_list}/counters.txt'";
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

            return (errCode, result);

        }

        public static (int, string) GenerateAllReports(List<string> hosts, AgentConfig agentConfig)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd /home/{agentConfig.User}/signalr-bench/; sh gen_all_report.sh; sh publish_report.sh; sh gen_summary.sh;";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            // TODO: hardcode for now
            Util.Log($"Report: http://wanlsignalrbenchserver.eastus.cloudapp.azure.com:8000/" + Environment.GetEnvironmentVariable("result_root") + "/all.html");

            return (errCode, result);

        }
        public static (int, string) GenerateSingleReport(List<string> hosts, AgentConfig agentConfig)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd /home/{agentConfig.User}/signalr-bench/; sh gen_html.sh;";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            // TODO: hardcode for now
            Util.Log($"Report: http://wanlsignalrbenchserver.eastus.cloudapp.azure.com:8000/" + Environment.GetEnvironmentVariable("result_root") + "/all.html");

            return (errCode, result);
        }
    }


}

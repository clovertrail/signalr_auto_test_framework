using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JenkinsScript
{
    class ShellHelper
    {
        public static void HandleResult(int errCode, string result)
        {
            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }
            return;
        }

        public static (int, string) Bash(string cmd, bool wait=true, bool handleRes=false)
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

            if (handleRes == true)
            {
                HandleResult(errCode, result);
            }

            return (errCode, result);
        }

        public static (int, string) RemoteBash(string user, string host, int port, string password, string cmd, bool wait = true, bool handleRes = false, int retry = 1)
        {

            int errCode = 0;
            string result = "";
            for (var i = 0; i < retry; i++)
            {
                if (host.IndexOf("localhost") >= 0 || host.IndexOf("127.0.0.1") >= 0) return Bash(cmd, wait);
                Util.Log($"password: {password}");
                Util.Log($"port: {port}");
                Util.Log($"host: {host}");
                Util.Log($"cmd: {cmd}");
                string sshPassCmd = $"sshpass -p {password} ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
                Util.Log($"SSH Pass Cmd: {sshPassCmd}");
                (errCode, result) = Bash(sshPassCmd, wait: wait, handleRes: retry > 1 && i < retry - 1? false: handleRes);
                if (errCode == 0) break;
                Util.Log($"retry {i+1}th time");
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            }


            return (errCode, result);
        }

        public static (int, string) KillAllDotnetProcess(List<string> hosts, AgentConfig agentConfig, ArgsOption argsOption)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            hosts.ForEach(host =>
            {
                cmd = $"killall dotnet || true";
                if (host.Contains("localhost") || host.Contains("127.0.0.1"))
                {
                    if (argsOption.Debug.Contains("debug") && argsOption.Debug.Contains("local"))
                    {
                        Util.Log($"CMD: {agentConfig.User}@{host}: {cmd}");
                        (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, host, agentConfig.SshPort, agentConfig.Password, cmd);
                    }
                }
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

            if (argsOption.Debug.Contains("local"))
                cmd = $"cd /home/{agentConfig.User}/workspace/signalr_auto_test_framework_x/signalr_bench/AppServer/; export AzureSignalRConnectionString='{argsOption.AzureSignalrConnectionString}'; dotnet run > log_appserver.txt";
            else
                cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/AppServer/; export AzureSignalRConnectionString='{argsOption.AzureSignalrConnectionString}'; dotnet run > log_appserver.txt";
            Util.Log($"{agentConfig.User}@{agentConfig.AppServer}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.AppServer, agentConfig.SshPort, agentConfig.Password, cmd, wait: false);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);

        }

        public static (int, string) StartRpcSlaves(AgentConfig agentConfig, ArgsOption argsOption)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            agentConfig.Slaves.ForEach(host =>
            {
                if (argsOption.Debug.Contains("local"))
                    cmd = $"cd /home/{agentConfig.User}/workspace/signalr_auto_test_framework_x/signalr_bench/Rpc/Bench.Server/; dotnet run -- --rpcPort {agentConfig.RpcPort} -d 0.0.0.0 > log_rpcslave.txt";
                else
                    cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Server/; dotnet run -- --rpcPort {agentConfig.RpcPort} -d 0.0.0.0 > log_rpcslave.txt";
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

        public static (int, string) StartRpcMaster(AgentConfig agentConfig, 
            ArgsOption argsOption, string serviceType, bool isSelfHost, string transportType, string hubProtocol, string scenario,
            int connection, int duration, int interval, string pipeLine, BenchmarkVmBuilder vmCreator)
        {
            Util.Log($"service type: {serviceType}, transport type: {transportType}, hub protocol: {hubProtocol}, scenario: {scenario}");
            var errCode = 0;
            var result = "";
            var cmd = "";

            var bench_type_list = serviceType;
            var bench_codec_list = hubProtocol;
            var bench_name_list = scenario;
            var maxRetry = 1;
            var slaveList = "";

            for (var i = 0; i < agentConfig.Slaves.Count; i++)
            {
                slaveList += agentConfig.Slaves[i];
                if (i < agentConfig.Slaves.Count - 1)
                    slaveList += ";";
            }

            var serverUrl = vmCreator.AppSvrDomainName();
            if (argsOption.Debug.Contains("debug"))
            {
                serverUrl = "wanlauto5c54189495appsvrdns0.southeastasia.cloudapp.azure.com";
                if (argsOption.Debug.Contains("local"))
                {
                    serverUrl = "localhost";
                }
            }

            for (var i = 0; i < 1; i++)
            {
                var clear = "false";
                var outputCounterDir = "";
                var outputCounterFile = "";
                if (argsOption.Debug.Contains("local"))
                {
                    cmd = $"cd /home/{agentConfig.User}/workspace/signalr_auto_test_framework_x/signalr_bench/Rpc/Bench.Client/; ";
                    outputCounterDir = $"/home/{agentConfig.User}/workspace/signalr_auto_test_framework_x/signalr_bench/Report/public/results/{Environment.GetEnvironmentVariable("result_root")}/{bench_type_list}_{transportType}_{bench_codec_list}_{bench_name_list}_{connection}/";
                    outputCounterFile = outputCounterDir + $"counters.txt";
                }
                else
                {
                    cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; ";
                    outputCounterDir = $"/home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Report/public/results/{Environment.GetEnvironmentVariable("result_root")}/{bench_type_list}_{transportType}_{bench_codec_list}_{bench_name_list}_{connection}/";
                    outputCounterFile = outputCounterDir + $"counters.txt";
                }
                cmd += $"rm -rf {outputCounterFile} || true;";

                cmd += $"export bench_type_list='{serviceType}{connection}'; " +
                    $"export bench_codec_list='{hubProtocol}'; " +
                    $"export bench_name_list='{scenario}'; ";

                cmd += $"dotnet build; dotnet run -- " +
                    $"--rpcPort 5555 " +
                    $"--duration {duration} --connections {connection} --interval {interval} --slaves {agentConfig.Slaves.Count} --serverUrl 'http://{serverUrl}:5000/signalrbench' --pipeLine '{string.Join(";", pipeLine)}' " +
                    $"-v {serviceType} -t {transportType} -p {hubProtocol} -s {scenario} " +
                    $" --slaveList '{slaveList}' " +
                    $" --retry {0} " +
                    $" --clear {clear} " +
                    $"-o '{outputCounterFile}' > log_rpcmaster.txt";

                Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
                (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);
                if (errCode == 0) break;
                Util.Log($"retry {i}th time");
                //Task.Delay(10000).Wait();

                if (errCode != 0)
                {
                    Util.Log($"ERR {errCode}: {result}");
                }
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
        public static (int, string) GenerateSingleReport(List<string> hosts, AgentConfig agentConfig, 
            string serviceType, string transportType, string hubProtocol, string scenario, int connections)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd /home/{agentConfig.User}/signalr-bench/; " +
                $"export bench_type_list='{serviceType}{connections}'; export bench_codec_list='{hubProtocol}'; export bench_name_list='{scenario}'; " + 
                $"export OnlineConnections={connections}; export ActiveConnections=1000; " +
                $"sh gen_html.sh;";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);
        }

        public static (int, string) CreateSignalrService(ArgsOption argsOption, int unitCount)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob("SignalrConfigFileName");
            Console.WriteLine($"content: {content}");
            var config = AzureBlobReader.ParseYaml<SignalrConfig>(content);

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log($"CMD: signalr service: az login");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            // change subscription
            cmd = $"az account set --subscription {config.Subscription}";
            Util.Log($"CMD: az account set --subscription");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            var rnd = new Random();
            var SrRndNum = (rnd.Next(10000) * rnd.Next(10000)).ToString();

            var groupName = config.BaseName + "Group";
            var srName = config.BaseName + SrRndNum + "SR";
            
            cmd = $"  az extension add -n signalr || true";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            
            // create resource group
            cmd = $"  az group create --name {groupName} --location {config.Location}";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            

            //create signalr service
            cmd = $"az signalr create --name {srName} --resource-group {groupName}  --sku {config.Sku} --unit-count {unitCount} --query hostName -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            var signalrHostName = result;
            Console.WriteLine($"signalrHostName: {signalrHostName}");

            // get access key
            cmd = $"az signalr key list --name {srName} --resource-group {groupName} --query primaryKey -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            var signalrPrimaryKey = result;
            Console.WriteLine($"signalrPrimaryKey: {signalrPrimaryKey}");

            // combine to connection string
            signalrHostName = signalrHostName.Substring(0, signalrHostName.Length - 1);
            signalrPrimaryKey = signalrPrimaryKey.Substring(0, signalrPrimaryKey.Length - 1);
            var connectionString = $"Endpoint=https://{signalrHostName};AccessKey={signalrPrimaryKey};";
            Console.WriteLine($"connection string: {connectionString}");
            ShellHelper.Bash($"export AzureSignalRConnectionString='{connectionString}'", handleRes: true);
            return (errCode, connectionString);
        }

        public static (int, string) DeleteSignalr(ArgsOption args)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = AzureBlobReader.ReadBlob("SignalrConfigFileName");
            var config = AzureBlobReader.ParseYaml<SignalrConfig>(content);

            var groupName = config.BaseName + "Group";

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log($"CMD: signalr service: logint azure");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            // delete resource group
            cmd = $"az group delete --name {groupName} --yes";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            return (errCode, result);
        }

        

    }


}

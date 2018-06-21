using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

        public static (int, string) RemoteBash(string user, string host, int port, string password, string cmd, bool wait = true, bool handleRes = false)
        {
            if (host.IndexOf("localhost") >= 0 || host.IndexOf("127.0.0.1") >= 0) return Bash(cmd, wait);
            string sshPassCmd = $"ssh -p {port} -o StrictHostKeyChecking=no {user}@{host} \"{cmd}\"";
            return Bash(sshPassCmd, wait: wait, handleRes: handleRes);
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

        public static (int, string) StartRpcMaster(List<string> hosts, AgentConfig agentConfig, 
            ArgsOption argsOption, string serviceType, string transportType, string hubProtocol, string scenario)
        {
            Util.Log($"service type: {serviceType}, transport type: {transportType}, hub protocol: {hubProtocol}, scenario: {scenario}");
            var errCode = 0;
            var result = "";
            var cmd = "";

            var bench_type_list = serviceType;
            var bench_codec_list = hubProtocol;
            var bench_name_list = scenario;
            cmd = $"cd /home/{agentConfig.User}/signalr_auto_test_framework/signalr_bench/Rpc/Bench.Client/; export bench_type_list='{serviceType}'; export bench_codec_list='{hubProtocol}'; export bench_name_list='{scenario}'; export ConfigBlobContainerName='{argsOption.ContainerName}'; export AgentConfigFileName='{argsOption.AgentBlobName}';  export JobConfigFileName='{argsOption.JobBlobName}'; dotnet run -- -v {serviceType} -t {transportType} -p {hubProtocol} -s {scenario} -a '{argsOption.AgentConfigFile}' -j '{argsOption.JobConfigFile}' -o '/home/{agentConfig.User}/signalr-bench/{Environment.GetEnvironmentVariable("result_root")}/{bench_type_list}_{bench_codec_list}_{bench_name_list}/counters.txt'";
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
        public static (int, string) GenerateSingleReport(List<string> hosts, AgentConfig agentConfig, string serviceType, string transportType, string hubProtocol, string scenario)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            cmd = $"cd /home/{agentConfig.User}/signalr-bench/; export bench_type_list='{serviceType}'; export bench_codec_list='{hubProtocol}'; export bench_name_list='{scenario}'; sh gen_html.sh;";
            Util.Log($"CMD: {agentConfig.User}@{agentConfig.Master}: {cmd}");
            (errCode, result) = ShellHelper.RemoteBash(agentConfig.User, agentConfig.Master, agentConfig.SshPort, agentConfig.Password, cmd);

            if (errCode != 0)
            {
                Util.Log($"ERR {errCode}: {result}");
                Environment.Exit(1);
            }

            return (errCode, result);
        }

        static string SrRndNum = "";
        public static (int, string) CreateSignalrService(ArgsOption argsOption)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = ReadBlob("SignalrConfigFileName");
            var config = ParseYaml<SignalrConfig>(content);

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant};";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            var rnd = new Random();
            SrRndNum = (rnd.Next(10000) * rnd.Next(10000)).ToString();

            var groupName = config.Basename + SrRndNum + "Group";
            var srName = config.Basename + SrRndNum + "SR";

            // create resource group
            cmd = $"  az group create --name {groupName} --location {config.Location}";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            

            //create signalr service
            cmd = $"az signalr create --name {srName} --resource-group {srName}  --sku {config.Sku} --unit-count {config.UnitCount} --query hostName -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            var signalrHostName = result;

            // get access key
            cmd = $"az signalr key list --name {srName} --resource - group {groupName} --query primaryKey -o tsv";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);
            var signalrPrimaryKey = result;

            // combine to connection string
            var connectionString = $"Endpoint=https://{signalrHostName};AccessKey={signalrPrimaryKey};";
            Console.WriteLine($"connection string: {connectionString}");

            return (errCode, result);
        }

        public static (int, string) DeleteResourceGroup(ArgsOption args)
        {
            var errCode = 0;
            var result = "";
            var cmd = "";

            var content = ReadBlob("SignalrConfigFileName");
            var config = ParseYaml<SignalrConfig>(content);

            var groupName = config.Basename + SrRndNum + "Group";

            // login to azure
            cmd = $"az login --service-principal --username {config.AppId} --password {config.Password} --tenant {config.Tenant}";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            // delete resource group
            cmd = $"az group delete --name {groupName}";
            Util.Log($"CMD: signalr service: {cmd}");
            (errCode, result) = ShellHelper.Bash(cmd, handleRes: true);

            return (errCode, result);
        }
        
        private static string ReadBlob(string configKey)
        {
            // load signalr config
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            var content = "";

            if (CloudStorageAccount.TryParse(Environment.GetEnvironmentVariable("AzureStorageConnectionString"), out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(Environment.GetEnvironmentVariable("ConfigBlobContainerName"));
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(Environment.GetEnvironmentVariable(configKey));
                    content = cloudBlockBlob.DownloadTextAsync().GetAwaiter().GetResult();

                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);

                }
            }
            return content;
        }

        private static T ParseYaml<T>(string content)
        {
            var input = new StringReader(content);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var config = deserializer.Deserialize<T>(input);
            return config;
        }


    }


}

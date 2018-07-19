using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
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
            AgentConfig agentConfig = new AgentConfig();
            JobConfig jobConfig = new JobConfig();
            //List<string> hosts = new List<string>();

            if (argsOption.AgentConfigFile != null && argsOption.JobConfigFile != null)
            {
                var configLoader = new ConfigLoader();
                agentConfig = configLoader.Load<AgentConfig>(argsOption.AgentConfigFile);
                jobConfig = configLoader.Load<JobConfig>(argsOption.JobConfigFile);
                Util.Log("finish loading config");

                //hosts = new List<string>();
                //hosts.Add(agentConfig.AppServer);
                //agentConfig.Slaves.ForEach(slv => hosts.Add(slv));
                //hosts.Add(agentConfig.Master);
            }
            

            var errCode = 0;
            var result = "";
            var azureManager = new AzureManager();
            var vmBuilder = new BenchmarkVmBuilder(agentConfig);

            switch (argsOption.Step)
            {
                //case "KillAllDotnet":
                //    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                //    break;
                //case "CloneRepo":
                //    (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);
                //    break;
                //case "StartAppServer":
                //    (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                //    break;
                case "StartRpcServer":
                    (errCode, result) = ShellHelper.StartRpcSlaves(agentConfig, argsOption);
                    break;
                case "CreateSignalr":
                    (errCode, result) = ShellHelper.CreateSignalrService(argsOption, 10);
                    break;
                case "DeleteSignalr":
                    (errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                    break;
                case "CreateAllAgentVMs":
                    vmBuilder.CreateAgentVmsCore();
                    break;
                case "DeleteAllAgentVMs":
                    azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                    break;
                case "CreateAppServerVm":
                    vmBuilder.CreateAppServerVmCore();
                    break;
                case "CreateDogfoodSignalr":
                    break;
                case "RegisterDogfoodCloud":
                    DogfoodSignalROps.RegisterDogfoodCloud();
                    break;
                case "UnregisterDogfoodCloud":
                    DogfoodSignalROps.UnregisterDogfoodCloud();
                    break;
                case "All": 
                default:

                    if (argsOption.Debug.Contains("debug"))
                    {
                        if (argsOption.Debug.Contains("local"))
                        {
                            agentConfig.AppServer = "localhost";
                            agentConfig.Slaves = new List<string>();
                            agentConfig.SlaveVmCount = 1;
                            for (var i = 0; i < agentConfig.SlaveVmCount; i++)
                            {
                                agentConfig.Slaves.Add($"localhost");
                            }
                        }
                        else
                        {
                            agentConfig.AppServer = "wanlauto5c54189495appsvrdns0.southeastasia.cloudapp.azure.com";
                            agentConfig.Slaves = new List<string>();
                            agentConfig.SlaveVmCount = 2;
                            for (var i = 0; i < agentConfig.SlaveVmCount; i++)
                            {
                                agentConfig.Slaves.Add($"wanlauto5c54189495dns{i}.southeastasia.cloudapp.azure.com");
                            }
                        }
                        
                    }
                    else
                    {
                        while (true)
                        {
                            try
                            {
                                var createResourceTasks = new List<Task>();
                                createResourceTasks.Add(vmBuilder.CreateAppServerVm());
                                createResourceTasks.Add(vmBuilder.CreateAgentVms());
                                Task.WhenAll(createResourceTasks).Wait();

                            }
                            catch (Exception ex)
                            {
                                Util.Log($"creating VMs Exception: {ex}");
                                azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                                azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                                continue;
                            }
                            break;
                        }

                        agentConfig.AppServer = vmBuilder.AppSvrDomainName();
                        agentConfig.Slaves = new List<string>();
                        for (var i = 0; i < agentConfig.SlaveVmCount; i++)
                        {
                            agentConfig.Slaves.Add(vmBuilder.SlaveDomainName(i));
                        }
                    }
                    

                    var hosts = new List<string>();
                    hosts.Add(agentConfig.AppServer);
                    agentConfig.Slaves.ForEach(slv => hosts.Add(slv));
                    hosts.Add(agentConfig.Master);

                    // TODO: check if ssh success
                    Task.Delay(20 * 1000).Wait();

                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig, argsOption);
                    if (!argsOption.Debug.Contains("debug")) (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);


                    var types = jobConfig.ServiceTypeList;
                    var isSelfHost = true;
                    if (jobConfig.ServiceTypeList == null || jobConfig.ServiceTypeList.Count == 0)
                    {
                        types = jobConfig.SignalrUnit;
                        isSelfHost = false;
                    }

                    int indType = 0;
                    foreach (var serviceType in types)
                    {
                        var unit = 1;
                        unit = Convert.ToInt32(serviceType.Substring(4));

                        if (argsOption.Debug.Contains("debug"))
                        {
                            argsOption.AzureSignalrConnectionString = "Endpoint=https://wanldebugsa.service.signalr.net;AccessKey=4bb4D1/C8ocS8PhhNB4t71p6JN5AeNCvbDvRvx6wiuY=;";
                            if (argsOption.Debug.Contains("local"))
                            {
                                ShellHelper.Bash(" cd /home/wanl/workspace/oss/src/Microsoft.Azure.SignalR.ServiceRuntime; dotnet run  > /home/wanl/workspace/scripts-for-sr-benchmark/local/log_service.txt", wait: false);
                                argsOption.AzureSignalrConnectionString = "Endpoint=http://localhost;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789;";
                            }
                        }
                        else
                        {
                            while (true)
                            {

                                try
                                {
                                    var createSignalrR = Task.Run(() => { (errCode, argsOption.AzureSignalrConnectionString) = ShellHelper.CreateSignalrService(argsOption, unit); });
                                    Task.WhenAll(createSignalrR).Wait();
                                }
                                catch (Exception ex)
                                {
                                    Util.Log($"Creating SignalR Exception: {ex}");
                                    (errCode, result) = ShellHelper.DeleteSignalr(argsOption); // TODO what if delete fail
                                    continue;
                                }
                                break;
                            }
                        }

                        foreach (var transportType in jobConfig.TransportTypeList)
                        {
                            foreach (var hubProtocol in jobConfig.HubProtocolList)
                            {
                                foreach (var scenario in jobConfig.ScenarioList)
                                {
                                    var propName = scenario.First().ToString().ToUpper() + scenario.Substring(1); 
                                    var connectionBase = (jobConfig.ConnectionBase.GetType().GetProperty(propName).GetValue(jobConfig.ConnectionBase) as List<int>)[indType];
                                    var connectionIncreaseStep = (jobConfig.ConnectionIncreaseStep.GetType().GetProperty(propName).GetValue(jobConfig.ConnectionIncreaseStep) as List<int>)[indType];

                                    for (var connection = connectionBase; connection < connectionBase + connectionIncreaseStep * jobConfig.ConnectionLength; connection += connectionIncreaseStep)
                                    {
                                        var maxRetry = 1;
                                        var errCodeMaster = 0;
                                        for (var i = 0; i < maxRetry; i++)
                                        {
                                            int waitTime = 60000;
                                            if (argsOption.Debug.Contains("debug"))
                                            {
                                                waitTime = 5000;
                                            }
                                            Util.Log($"current connection: {connection}, duration: {jobConfig.Duration}, interval: {jobConfig.Interval}, transport type: {transportType}, protocol: {hubProtocol}, scenario: {scenario}");
                                            Task.Delay(waitTime).Wait();
                                            (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig, argsOption);
                                            if (argsOption.Debug.Contains("debug") && argsOption.Debug.Contains("local"))
                                            {
                                                Task.Delay(waitTime).Wait();
                                                ShellHelper.Bash(" cd /home/wanl/workspace/oss/src/Microsoft.Azure.SignalR.ServiceRuntime; dotnet run  > /home/wanl/workspace/scripts-for-sr-benchmark/local/log_service.txt", wait: false);
                                            }
                                            Task.Delay(waitTime).Wait();
                                            (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                                            Task.Delay(waitTime).Wait();
                                            (errCode, result) = ShellHelper.StartRpcSlaves(agentConfig, argsOption);
                                            Task.Delay(waitTime).Wait();
                                            (errCodeMaster, result) = ShellHelper.StartRpcMaster(agentConfig, argsOption,
                                                serviceType, isSelfHost, transportType, hubProtocol, scenario, connection, jobConfig.Duration,
                                                jobConfig.Interval, string.Join(";", jobConfig.Pipeline), vmBuilder);
                                            if (errCodeMaster == 0) break;
                                        }
                                    }
                                }
                            }
                        }
                        indType++;
                        if (argsOption.Debug.Contains("debug"))
                        {
                        }
                        else
                        {
                            (errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                        }
                        //(errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                    }
                    //(errCode, result) = ShellHelper.GenerateAllReports(hosts, agentConfig);

                    //azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                    //azureManager.DeleteResourceGroup(vmBuilder.AppSvrGroupName);
                    break;
            }

        }
    }
}

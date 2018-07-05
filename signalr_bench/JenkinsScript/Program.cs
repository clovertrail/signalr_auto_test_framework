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
            AgentConfig agentConfig = new AgentConfig();
            JobConfig jobConfig = new JobConfig();
            List<string> hosts = new List<string>();

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
                case "KillAllDotnet":
                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                    break;
                case "CloneRepo":
                    (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);
                    break;
                case "StartAppServer":
                    (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                    break;
                case "StartRpcServer":
                    (errCode, result) = ShellHelper.StartRpcSlaves(hosts, agentConfig, argsOption);
                    break;
                case "CreateSignalr":
                    (errCode, result) = ShellHelper.CreateSignalrService(argsOption);
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
                case "All": 
                default:
                    var createSignalrR = Task.Run(()=> { (errCode, result) = ShellHelper.CreateSignalrService(argsOption); });

                    argsOption.AzureSignalrConnectionString = result;
                    Util.Log($"signalr connection string {argsOption.AzureSignalrConnectionString}");
                    var createResourceTasks = new List<Task>();
                    createResourceTasks.Add(vmBuilder.CreateAgentVms());
                    createResourceTasks.Add(vmBuilder.CreateAppServerVm());
                    createResourceTasks.Add(createSignalrR);

                    Task.WhenAll(createResourceTasks).Wait();

                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                    (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);


                    var types = jobConfig.ServiceTypeList;
                    if (jobConfig.ServiceTypeList == null || jobConfig.ServiceTypeList.Count == 0)
                        types = jobConfig.SignalrUnit;

                    int indType = 0;
                    foreach (var serviceType in types)
                    {
                        foreach (var transportType in jobConfig.TransportTypeList)
                        {
                            foreach (var hubProtocol in jobConfig.HubProtocolList)
                            {
                                foreach (var scenario in jobConfig.ScenarioList)
                                {
                                    var connectionBase = jobConfig.ConnectionBase[indType];
                                    var connectionIncreaseStep = jobConfig.ConnectionIncreaseStep[indType];

                                    //for (var connection = connectionBase; ; connection += connectionIncreaseStep)
                                    // TODO: debug
                                    for (var connection = connectionBase; connection < connectionBase + connectionIncreaseStep + 10; connection += connectionIncreaseStep)
                                    {
                                        (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                                        (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                                        Task.Delay(5000).Wait();
                                        (errCode, result) = ShellHelper.StartRpcSlaves(hosts, agentConfig, argsOption);
                                        Task.Delay(20000).Wait();
                                        (errCode, result) = ShellHelper.StartRpcMaster(hosts, agentConfig, argsOption,
                                            serviceType, transportType, hubProtocol, scenario, connection, jobConfig.Duration,
                                            jobConfig.Interval, string.Join(";", jobConfig.Pipeline), vmBuilder);
                                        if (errCode != 0) break;
                                        (errCode, result) = ShellHelper.GenerateSingleReport(hosts, agentConfig,
                                            serviceType, transportType, hubProtocol, scenario, connection);
                                    }
                                }
                            }
                        }
                        indType++;
                    }
                    (errCode, result) = ShellHelper.GenerateAllReports(hosts, agentConfig);

                    (errCode, result) = ShellHelper.DeleteSignalr(argsOption);
                    azureManager.DeleteResourceGroup(vmBuilder.GroupName);
                    break;
            }

        }
    }
}

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
            
            switch(argsOption.Step)
            {
                case "killalldotnet":
                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                    break;
                case "clonerepo":
                    (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);
                    break;
                case "startappserver":
                    (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                    break;
                case "startrpcserver":
                    (errCode, result) = ShellHelper.StartRpcSlaves(hosts, agentConfig, argsOption);
                    break;
                case "startrpcmaster":
                    (errCode, result) = ShellHelper.StartRpcMaster(hosts, agentConfig, argsOption, "", "", "", "");
                    break;
                case "generatereport":
                    (errCode, result) = ShellHelper.GenerateAllReports(hosts, agentConfig);
                    break;
                case "all": 
                default:
                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                    (errCode, result) = ShellHelper.GitCloneRepo(hosts, agentConfig);

                    foreach (var serviceType in jobConfig.ServiceTypeList)
                    {
                        foreach (var transportType in jobConfig.TransportTypeList)
                        {
                            foreach (var hubProtocol in jobConfig.HubProtocolList)
                            {
                                foreach (var scenario in jobConfig.ScenarioList)
                                {
                                    (errCode, result) = ShellHelper.KillAllDotnetProcess(hosts, agentConfig);
                                    (errCode, result) = ShellHelper.StartAppServer(hosts, agentConfig, argsOption);
                                    Task.Delay(5000).Wait();
                                    (errCode, result) = ShellHelper.StartRpcSlaves(hosts, agentConfig, argsOption);
                                    Task.Delay(20000).Wait();
                                    (errCode, result) = ShellHelper.StartRpcMaster(hosts, agentConfig, argsOption, serviceType, transportType, hubProtocol, scenario);
                                    (errCode, result) = ShellHelper.GenerateSingleReport(hosts, agentConfig);
                                }
                            }
                        }
                    }
                    (errCode, result) = ShellHelper.GenerateAllReports(hosts, agentConfig);
                    break;
            }

        }
    }
}

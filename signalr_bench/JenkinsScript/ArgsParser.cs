using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    public class ArgsOption
    {
        [Option('a', "agentconfig", Required = true, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option('h', "help", Required = false, HelpText = " dotnet run -j /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/job.yaml -a  /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/agent.yaml")]
        public string Help { get; set; }
    }
}
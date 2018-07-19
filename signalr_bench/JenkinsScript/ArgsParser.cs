using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
{
    public class ArgsOption
    {
        [Option('a', "agentconfig", Required = false, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option('C', "containername", Required = false, HelpText = "Specify Azure Container Name")]
        public string ContainerName { get; set; }

        [Option('J', "jobblobname", Required = false, HelpText = "Specify Azure Blob Name For Job Config File")]
        public string JobBlobName { get; set; }

        [Option('A', "agentblobname", Required = false, HelpText = "Specify Azure Blob Name For Agent Config File")]
        public string AgentBlobName { get; set; }

        [Option('s', "azuresignalr", Required = false, HelpText = "Specify Azure Signalr connection string")]
        public string AzureSignalrConnectionString { get; set; }

        [Option('o', "outputcounterfile", Required = false, HelpText = "Specify Output File For Counters")]
        public string OutputCounterFile { get; set; }

        [Option('S', "step", Required = false, HelpText = "Specify the Step")]
        public string  Step{ get; set; }

        [Option('p', "spblobname", Required = false, HelpText = "Specify the Service Principal")]
        public string SpBlobName { get; set; }

        [Option('h', "help", Required = false, HelpText = " dotnet run -j /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/job.yaml -a  /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/agent.yaml")]
        public string Help { get; set; }

        [Option("debug", Required = false, HelpText = " debug")]
        public string Debug { get; set; }

    }
}
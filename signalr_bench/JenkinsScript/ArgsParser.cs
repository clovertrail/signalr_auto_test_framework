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

        [Option('S', "step", Required = false, HelpText = "Specify the step")]
        public string  Step{ get; set; }

        // for signalr service
        //[Option('l', "location", Required = false, HelpText = "Specify Location for Signalr Service")]
        //public string Location { get; set; }

        //[Option('i', "appid", Required = false, HelpText = "Specify AppId for Signalr Service")]
        //public string AppId { get; set; }

        //[Option('p', "password", Required = false, HelpText = "Specify Password for Signalr Service")]
        //public string Password { get; set; }

        //[Option('t', "tenant", Required = false, HelpText = "Specify Tenant for Signalr Service")]
        //public string Tenant { get; set; }

        //[Option('R', "signalrconfig", Required = false, HelpText = "Specify Config file for signalr service")]
        //public string SignalrConfig { get; set; }

        [Option('h', "help", Required = false, HelpText = " dotnet run -j /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/job.yaml -a  /home/wanl/workspace/signalr_auto_test_framework/signalr_bench/Rpc/Configs/agent.yaml")]
        public string Help { get; set; }


    }
}
using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Common
{
    public class ArgsOption
    {
        [Option('a', "agentconfig", Required = true, HelpText = "Specify Agent Config File")]
        public string AgentConfigFile { get; set; }

        [Option('j', "jobconfig", Required = false, HelpText = "Specify Job Config File")]
        public string JobConfigFile { get; set; }

        [Option('d', "dnsname", Required = false, HelpText = "Specify DNS Name")]
        public string DnsName { get; set; }

        [Option('c', "containername", Required = false, HelpText = "Specify Azure Container Name")]
        public string ContainerName { get; set; }

        [Option('y', "jobblobname", Required = false, HelpText = "Specify Azure Blob Name For Job Config File")]
        public string JobBlobName { get; set; }

        [Option('x', "agentblobname", Required = false, HelpText = "Specify Azure Blob Name For Agent Config File")]
        public string AgentBlobName { get; set; }

        [Option('o', "outputcounterfile", Required = false, HelpText = "Specify Output File For Counters")]
        public string OutputCounterFile { get; set; }

        [Option('v', "servicetype", Required = false, HelpText = "Specify BenchMark Service Type")]
        public string ServiceType { get; set; }

        [Option('t', "transporttype", Required = false, HelpText = "Specify TransportType")]
        public string TransportType { get; set; }

        [Option('p', "hubprotocol", Required = false, HelpText = "Specify BenchMark Hub Protocol")]
        public string HubProtocal { get; set; }

        [Option('s', "scenerio", Required = false, HelpText = "Specify BenchMark Scenario")]
        public string Scenario { get; set; }

    }
}
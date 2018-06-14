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
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace ManageVMs
{
    class ArgsOption
    {
        [Option('c', "vmcount", Required = false, HelpText = "Specify VM Count")]
        public string VmCount { get; set; }

        [Option('p', "prefix", Required = false, HelpText = "Specify VM Prefix for vm and groups")]
        public string Prefix { get; set; }

        [Option('p', "authfile", Required = false, HelpText = "Specify Auth File")]
        public string AuthFile { get; set; }
    }
}

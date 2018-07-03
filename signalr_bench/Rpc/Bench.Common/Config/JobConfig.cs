using Bench.Common.Config.MixConfigs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Common.Config
{
    public class JobConfig
    {
        // common config
        public int Connections { get; set; }
        public int Slaves { get; set; }
        public int Interval { get; set; }
        public int Duration { get; set; }
        public string ServerUrl { get; set; }
        public List<string> Pipeline { get; set; }

        public JobConfig(ArgsOption argsOption)
        {
            Connections = argsOption.Connections;
            Slaves = argsOption.Slaves;
            Interval = argsOption.Interval;
            Duration = argsOption.Duration;
            ServerUrl = argsOption.ServerUrl;
            Pipeline = new List<string>(argsOption.PipeLine.Split(';'));
        }

        public JobConfig() { }
    }
}

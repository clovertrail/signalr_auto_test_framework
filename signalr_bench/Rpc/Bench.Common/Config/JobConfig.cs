﻿using System;
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

        // benchmark matrix config
        public List<string> ServiceTypeList { get; set; }
        public List<string> HubProtocolList { get; set; }
        public List<string> TransportTypeList { get; set; }
        public List<string> ScenarioList { get; set; }
    }
}

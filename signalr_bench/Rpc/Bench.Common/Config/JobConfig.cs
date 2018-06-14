﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Common.Config
{
    public class JobConfig
    {
        public int Connections { get; set; }
        public int Interval { get; set; }
        public int Duration { get; set; }
        public string HubProtocol { get; set; }
        public string TransportType { get; set; }
        public List<string> Scenarios { get; set; }
        public string ServerUrl { get; set; }
        public List<string> Pipeline { get; set; }
        public string CallbackName { get; set; }
    }
}

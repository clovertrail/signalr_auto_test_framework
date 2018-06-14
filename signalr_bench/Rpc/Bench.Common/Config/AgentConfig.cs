using System;
using System.Collections.Generic;
using System.Text;

namespace Bench.Common.Config
{
    public class AgentConfig
    {
        public string Master { get; set; }
        public List<string> Slaves { get; set; }
        public string AppServer { get; set; }
        public int RpcPort { get; set; }
        public int SshPort { get; set; }
        public string User { get; set; }
        public string Repo { get; set; }
        public string Password { get; set; }
    }
}

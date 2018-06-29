using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript
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
        public string Prefix { get; set; }
        public string Location { get; set; }
        public string VmSize { get; set; }
        public string VmName { get; set; }
        public int VmCount { get; set; }
        public string VmPassWord { get; set; }
        public string Ssh { get; set; }

        
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace JenkinsScript.Config.FinerConfigs
{
    public class GroupConfig
    {
        public List<int> groupConnectionBase {get; set;}
        public List<int> groupConnectionStep {get; set;}
        public int groupConnectionLength {get; set;}
        public List<int> groupNumBase {get; set;}
        public List<int> groupNumStep {get; set;}
        public int groupNumLength {get; set;}
    }
}

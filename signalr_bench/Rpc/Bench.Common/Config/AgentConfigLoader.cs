using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bench.Common.Config
{
    public class AgentConfigLoader
    {
        public AgentConfig Load(string path)
        {
            var agentConfigContent = "";
            if (path.IndexOf("http") >= 0)
            {
                var client = new HttpClient();
                agentConfigContent = client.GetStringAsync(path).GetAwaiter().GetResult();
            }
            else
            {
                agentConfigContent = File.ReadAllText(path);
            }

            Util.Log($"agent config: {agentConfigContent}");
            return Parse(agentConfigContent);
        }

        public AgentConfig Parse(string yaml)
        {
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var config = deserializer.Deserialize<AgentConfig>(input);

            return config;
        }
    }
}

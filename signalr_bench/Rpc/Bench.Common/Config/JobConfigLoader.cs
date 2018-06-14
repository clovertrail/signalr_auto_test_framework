using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bench.Common.Config
{
    public class JobConfigLoader
    {
        public JobConfig Load(string path)
        {
            var jobConfigContent = "";
            if (path.IndexOf("http") >= 0)
            {
                var client = new HttpClient();
                jobConfigContent = client.GetStringAsync(path).GetAwaiter().GetResult();
            }
            else
            {
                jobConfigContent = File.ReadAllText(path);
            }
            Util.Log($"job config: {jobConfigContent}");
            return Parse(jobConfigContent);
        }

        public JobConfig Parse(string yaml)
        {
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var config = deserializer.Deserialize<JobConfig>(input);

            return config;
        }
    }
}

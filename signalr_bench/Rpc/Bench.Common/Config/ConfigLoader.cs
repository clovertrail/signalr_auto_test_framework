using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bench.Common.Config
{
    public class ConfigLoader
    {
        public T Load<T>(string path)
        {
            var content = ReadFile<T>(path);
            return Parse<T>(content);
        }

        private string ReadFile<T>(string path)
        {
            //Util.Log($"Config Path: {path}");
            var content = "";
            if (path.Contains("DefaultEndpointsProtocol") && path.Contains("AccountKey") && path.Contains("EndpointSuffix"))
            {
                CloudStorageAccount storageAccount = null;
                CloudBlobContainer cloudBlobContainer = null;

                if (CloudStorageAccount.TryParse(path, out storageAccount))
                {
                    try
                    {
                        CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                        cloudBlobContainer = cloudBlobClient.GetContainerReference(Environment.GetEnvironmentVariable("ConfigBlobContainerName"));
                        string type = typeof(T) == typeof(AgentConfig)? Environment.GetEnvironmentVariable("AgentConfigFileName") : Environment.GetEnvironmentVariable("JobConfigFileName");
                        CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(type);
                        content = cloudBlockBlob.DownloadTextAsync().GetAwaiter().GetResult();

                    }
                    catch (StorageException ex)
                    {
                        Console.WriteLine("Error returned from the service: {0}", ex.Message);

                    }
                }

            }
            else if (path.Contains("http"))
            {
                var client = new HttpClient();
                content = client.GetStringAsync(path).GetAwaiter().GetResult();
            }
            else
            {
                content = File.ReadAllText(path);
            }

            //Util.Log($"Config: \n {content}");
            return content;
        }

        public T Parse<T>(string content)
        {
            var input = new StringReader(content);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var config = deserializer.Deserialize<T>(input);

            return config;
        }
    }
}

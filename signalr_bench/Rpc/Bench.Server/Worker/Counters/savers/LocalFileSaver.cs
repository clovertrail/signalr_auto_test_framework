using Bench.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bench.Server.Worker.Savers
{
    class LocalFileSaver : ISaver
    {
        public void Save(string url, long timestamp, ConcurrentDictionary<string, int> counters)
        {
            JObject jCounters = JObject.FromObject(counters);

            jCounters = Util.Sort(jCounters);

            var totalReceive = 0;
            foreach (var c in counters)
            {
                if (c.Key != "message:sent")
                {
                    totalReceive += c.Value;
                }
            }

            JObject rec = new JObject
            {
                { "Time", Timestamp2DateTimeStr(timestamp) },
                { "Counters", jCounters },
                {"totalSent", counters["message:sent"]},
                {"totalReceive", totalReceive }
            };
            string oneLineRecord = Regex.Replace(rec.ToString(), @"\s+", "");
            oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "") + Environment.NewLine;
            SaveFile(url, oneLineRecord);
        }

        private void SaveFile(string path, string content)
        {
            if (!File.Exists(path))
            {
                StreamWriter sw = File.CreateText(path);
            }
            File.AppendAllText(path, content);

        }

        private string Timestamp2DateTimeStr(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        
    }
}

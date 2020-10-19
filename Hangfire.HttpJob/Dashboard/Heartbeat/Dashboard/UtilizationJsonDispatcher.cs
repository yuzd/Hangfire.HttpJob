using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.Heartbeat.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Hangfire.Heartbeat.Dashboard
{
    internal sealed class UtilizationJsonDispatcher : IDashboardDispatcher
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new JsonConverter[] { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
        };

        public async Task Dispatch(DashboardContext context)
        {
            var servers = context.Storage.GetMonitoringApi().Servers();
            var serverUtilizationViews = new List<ServerView>(servers.Count);
            using (var connection = context.Storage.GetConnection())
            {
                foreach (var serverDto in servers)
                {
                    var key = Utils.FormatKey(serverDto.Name);
                    var hash = connection.GetAllEntriesFromHash(key);

                    if (hash == null) continue;

                    foreach (var hashValue in hash)
                    {
                        var processInfo = SerializationHelper.Deserialize<ProcessInfo>(hashValue.Value);

                        serverUtilizationViews.Add(new ServerView
                        {
                            Name = $"{serverDto.Name}:{processInfo.Id}",
                            DisplayName = serverDto.Name,
                            ProcessName = processInfo.ProcessName,
                            Timestamp = processInfo.Timestamp.ToUnixTimeMilliseconds(),
                            ProcessId = processInfo.Id.ToString(CultureInfo.InvariantCulture),
                            CpuUsagePercentage = processInfo.CpuUsage,
                            WorkingMemorySet = processInfo.WorkingSet,
                            DiskUsage = processInfo.DiskUsage,
                            Error = false,
                        });
                    }
                }

                if (!string.IsNullOrEmpty(ProcessMonitor.CurrentServerId))
                {
                    //获取jobagent的心跳包
                    var agentList = connection.GetAllEntriesFromHash("AgentHeart:"+ProcessMonitor.CurrentServerId);
                    if (agentList != null)
                    {
                        foreach (var agent in agentList)
                        {
                            var processInfo = SerializationHelper.Deserialize<ProcessInfo>(agent.Value);
                            serverUtilizationViews.Add(new ServerView
                            {
                                Name = agent.Key,//这个是唯一key
                                DisplayName = "Agent:" + processInfo.Server,
                                Error = ((DateTimeOffset.UtcNow - processInfo.Timestamp).TotalSeconds > 10),
                                ProcessName = processInfo.ProcessName,
                                Timestamp = processInfo.Timestamp.ToUnixTimeMilliseconds(),
                                ProcessId = processInfo.Id.ToString(CultureInfo.InvariantCulture),
                                CpuUsagePercentage = processInfo.CpuUsage,
                                WorkingMemorySet = processInfo.WorkingSet,
                                DiskUsage = processInfo.DiskUsage
                            });
                        }
                    }
                }
               

            }

            context.Response.ContentType = "application/json";
            var serialized = JsonConvert.SerializeObject(serverUtilizationViews, JsonSerializerSettings);
            await context.Response.WriteAsync(serialized);
        }

        private static string FormatServerName(string name)
        {
            var lastIndex = name.Length - 1;
            var occurrences = 0;

            for (var i = name.Length - 1; i > 0; i--)
            {
                if (name[i] == ':') occurrences++;
                if (occurrences == 2)
                {
                    lastIndex = i;
                    break;
                }
            }

            return lastIndex > 0 ? name.Substring(0, lastIndex) : name;
        }

        private static double ParseDouble(string s)
        {
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d);
            return d;
        }

        private static long ParseLong(string s)
        {
            long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i);
            return i;
        }
    }
}

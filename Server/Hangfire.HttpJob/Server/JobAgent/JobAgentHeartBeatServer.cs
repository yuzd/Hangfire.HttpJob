using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Heartbeat.Dashboard;
using Hangfire.Heartbeat.Server;
using Hangfire.HttpJob.Support;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.HttpJob.Server.JobAgent
{
    /// <summary>
    /// 处理jobagent通过storage上报的消息
    /// </summary>
    public sealed class JobAgentHeartBeatServer
    {
        /// <summary>
        /// 每隔2s获取一次
        /// </summary>
        private static System.Threading.Timer mDetectionTimer;

        /// <summary>
        /// 第一次先把agent的数据插入进去
        /// </summary>
        private static bool isInit = false;


        public static void Start(bool isFirst = true)
        {
            if (isFirst) mDetectionTimer = new System.Threading.Timer(OnVerify, null, 1000 * 5, 1000 * 5);
            OnVerify(null);
        }

        /// <summary>
        /// 获取agent服务器列表
        /// </summary>
        /// <returns></returns>
        internal static string GetAgentServerListHtml()
        {
            //拿到所有的周期性job
            var jobagentServerList = new Dictionary<string, ServerView>();
            using (var connection = JobStorage.Current.GetConnection())
            {
                
                var jobList = connection.GetRecurringJobs();
                foreach (var job in jobList)
                {
                    var httpJob = job.Job.Args.FirstOrDefault() as HttpJobItem;
                    //只处理agentjob
                    if (httpJob == null || string.IsNullOrEmpty(httpJob.AgentClass)) continue;
                    var uri = new Uri(httpJob.Url);
                    var key = uri.Host + ":" + uri.Port;
                    if (!jobagentServerList.ContainsKey(key))
                    {
                        jobagentServerList.Add(key, new ServerView
                        {
                            Name = key,//服务器
                            Error = true,
                            Timestamp = 1,
                            ProcessName = "waiting"//最后心跳时间
                        });
                    }
                    else
                    {
                        jobagentServerList[key].Timestamp++;//job的数量
                    }
                }

                if (!jobagentServerList.Any()) return "";
                if (!string.IsNullOrEmpty(ProcessMonitor.CurrentServerId))
                {
                    //获取jobagent的心跳包
                    var agentList = connection.GetAllEntriesFromHash("AgentHeart:" + ProcessMonitor.CurrentServerId);
                    if (agentList != null)
                    {
                        foreach (var agent in agentList)
                        {
                            var processInfo = SerializationHelper.Deserialize<ProcessInfo>(agent.Value);
                            if (!jobagentServerList.TryGetValue(agent.Key, out var view))
                            {
                                continue;
                            }

                            view.DisplayName = view.Timestamp + "";
                            view.Timestamp = processInfo.Timestamp.ToUnixTimeSeconds();
                            view.Error = ((DateTimeOffset.UtcNow - processInfo.Timestamp).TotalSeconds > 10);
                            view.ProcessName = processInfo.Timestamp.ToString("yyyy/MM/dd HH:mm:ss");

                        }
                    }
                }
            }
            //<span class=\"glyphicon glyphicon-ok text-success\" title=\"Active\"></span>\n"

            var rowTemplete = @"<tr>" +
                "                                        <td>\n" +
                "                                            @ERROR@" +
                "                                            <span class='labe label-defult text-uppercase' >@SERVER@</span>\n" +
                "                                        </td>\n" +
                "                                        <td>\n" +
                "                                           @JOBCOUNT@" +
                "                                        </td>\n" +
                "                                        <td>\n" +
                "                                            <span data-moment='@Timestamp@'>@TimeString@</span>\n" +
                "                                        </td>\n" +
                "                                    </tr>\n";

            var html = " <div class=\"col-md-12\">" +
                       "                        <h1 class=\"page-header\">Agent</h1>" +
                       "                        <div class=\"table-responsive\">\n" +
                       "                            <table class=\"table\">\n" +
                       "                                <thead>\n" +
                       "                                    <tr>\n" +
                       "                                        <th>Server</th>\n" +
                       "                                        <th>JobCount</th>\n" +
                       "                                        <th>HeartBeat</th>\n" +
                       "                                    </tr>\n" +
                       "                                </thead>\n" +
                       "                                <tbody>\n@TEMP@" +
                       "                                </tbody>" +
                       "                            </table>" +
                       "                        </div>" +
                       "                    </div>";

            var htmlList = new List<string>();
            foreach (var agentDetail in jobagentServerList)
            {
                var agent = agentDetail.Value;
                var agentInfo = rowTemplete.Replace("@ERROR@",
                        "<span class='glyphicon " +
                        (agent.Error ? "glyphicon-remove text-danger" : "glyphicon-ok text-success") + "' title'" +
                        (agent.Error ? "Waiting" : "Active") + "'></span>")
                    .Replace("@SERVER@", agent.Name)
                    .Replace("@JOBCOUNT@", agent.DisplayName + "")
                    .Replace("@Timestamp@", agent.Timestamp + "")
                    .Replace("@TimeString@", agent.ProcessName);

                htmlList.Add(agentInfo);
            }
            return html.Replace("@TEMP@",string.Join("\n", htmlList));
        }



        private static void OnVerify(object state)
        {
            mDetectionTimer.Change(-1, -1);
            if (string.IsNullOrEmpty(ProcessMonitor.CurrentServerId))
            {
                mDetectionTimer.Change(1000 * 1, 1000 * 1);
                return;
            }

            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                using (var lockStorage = connection.AcquireDistributedLock("JobAgentHeartbeat", TimeSpan.FromSeconds(30)))//防止多个server端竞争
                {
                    //拿到所有的周期性job
                    var jobagentServerList = new Dictionary<string, Tuple<string, string, string>>();
                    var jobList = connection.GetRecurringJobs();
                    foreach (var job in jobList)
                    {
                        var httpJob = job.Job.Args.FirstOrDefault() as HttpJobItem;
                        //只处理agentjob
                        if (httpJob == null || string.IsNullOrEmpty(httpJob.AgentClass)) continue;
                        var uri = new Uri(httpJob.Url);
                        var key = uri.Host + ":" + uri.Port;
                        if (!jobagentServerList.ContainsKey(key))
                        {
                            jobagentServerList.Add(key, new Tuple<string, string, string>(httpJob.Url, httpJob.BasicUserName, httpJob.BasicPassword));
                        }
                    }

                    if (!jobagentServerList.Any()) return;
                    IWriteOnlyTransaction writeTransaction = null;
                    if (!isInit)
                    {
                        writeTransaction = connection.CreateWriteTransaction();
                    }
                    var index = 1;
                    foreach (var jobagent in jobagentServerList)
                    {
                        if (!isInit)
                        {
                            var data = new ProcessInfo
                            {
                                Id = index,
                                Server = jobagent.Key,
                                ProcessName = "waiting...",
                                CpuUsage = 0.0,
                                WorkingSet = 0,
                                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2)
                            };
                            var values = new Dictionary<string, string>
                            {
                                [jobagent.Key] = SerializationHelper.Serialize(data)
                            };
                            writeTransaction?.SetRangeInHash("AgentHeart:" + ProcessMonitor.CurrentServerId, values);
                            index++;
                        }
                        new Task(async () =>
                        {
                            await SendHeartbeat(jobagent.Key,jobagent.Value.Item1, jobagent.Value.Item2, jobagent.Value.Item3);
                        }).Start();
                    }

                    if (!isInit)
                    {
                        isInit = true;
                        // if storage supports manual expiration handling
                        if (writeTransaction is JobStorageTransaction jsTransaction)
                        {
                            jsTransaction.ExpireHash("AgentHeart:" + ProcessMonitor.CurrentServerId, TimeSpan.FromMinutes(1));
                        }
                        writeTransaction?.Commit();
                        writeTransaction?.Dispose();
                    }

                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                mDetectionTimer.Change(1000 * 5, 1000 * 5);
            }
        }


        /// <summary>
        /// 给agent发送命令后 agent自己会上报数据到storage的
        /// </summary>
        /// <returns></returns>
        private static async Task SendHeartbeat(string agentKey,string url, string basicUserName, string basicPassword)
        {
            var agentServerId = string.Empty;
            try
            {
                var stroageString = HttpJob.GetJobStorage().Value;
                if (string.IsNullOrEmpty(stroageString))
                {
                    return;
                }

                HttpClient client = HangfireHttpClientFactory.Instance.GetHttpClient(url);
                var request = new HttpRequestMessage(new HttpMethod("Post"), url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(basicUserName) && !string.IsNullOrEmpty(basicPassword))
                {
                    var byteArray = Encoding.ASCII.GetBytes(basicUserName + ":" + basicPassword);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                }
                request.Headers.Add("x-job-storage", Convert.ToBase64String(Encoding.UTF8.GetBytes(stroageString)));
                request.Headers.Add("x-job-agent-action", "heartbeat");
                var uri = new Uri(url);
                request.Headers.Add("x-job-agent-server", uri.Host + ":" + uri.Port);
                if (!string.IsNullOrEmpty(ProcessMonitor.CurrentServerId))
                {
                    request.Headers.Add("x-job-server", Convert.ToBase64String(Encoding.UTF8.GetBytes(ProcessMonitor.CurrentServerId)));
                }
                var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                var content = await client.SendAsync(request, cancelToken.Token);

                //jobagent的话 在header里面有一个agentServerId
                agentServerId = content.Headers.GetValues("agentServerId").FirstOrDefault() ?? "";
               
            }
            catch (Exception)
            {
                //ignore agent挂了就到这
            }

            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                using (var writeTransaction = connection.CreateWriteTransaction())
                {
                    var values = new Dictionary<string, string>
                    {
                        [agentKey] = SerializationHelper.Serialize(new { id = agentServerId, time = DateTime.UtcNow })
                    };
                    var hashKey = "activeAgent:" + agentKey;
                    writeTransaction.SetRangeInHash(hashKey, values);
                    if (writeTransaction is JobStorageTransaction jsTransaction)
                    {
                        jsTransaction.ExpireHash(hashKey, TimeSpan.FromDays(1));
                    }
                    writeTransaction.Commit();
                }
            }
            catch (Exception)
            {
                //ignore
            }

        }
    }
}
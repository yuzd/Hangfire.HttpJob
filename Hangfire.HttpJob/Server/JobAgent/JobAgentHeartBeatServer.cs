using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Heartbeat.Server;
using Hangfire.HttpJob.Support;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.HttpJob.Server
{
    /// <summary>
    /// 处理jobagent通过storage上报的消息
    /// </summary>
    public sealed class JobAgentHeartBeatServer
    {
        /// <summary>
        /// 每隔2s获取一次
        /// </summary>
        private static  System.Threading.Timer mDetectionTimer ;


        public static void Start(bool isFirst = true)
        {
            if(isFirst) mDetectionTimer  = new System.Threading.Timer(OnVerify, null, 1000 *  5, 1000 *  5);
            OnVerify(null);
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
                using (var lockStorage = connection.AcquireDistributedLock("JobAgentHeartbeat",TimeSpan.FromSeconds(30)))//防止多个server端竞争
                {
                    //拿到所有的周期性job
                    var jobagentServerList = new Dictionary<string, Tuple<string, string, string>>();
                    var jobList = connection.GetRecurringJobs();
                    foreach (var job in jobList)
                    {
                        var httpJob = job.Job.Args.FirstOrDefault() as HttpJobItem;
                        //只处理agentjob
                        if(httpJob == null || string.IsNullOrEmpty(httpJob.AgentClass)) continue;
                        var uri = new Uri(httpJob.Url);
                        var key = uri.Host + ":" + uri.Port;
                        if (!jobagentServerList.ContainsKey(key))
                        {
                            jobagentServerList.Add(key,new Tuple<string,string, string>(httpJob.Url,httpJob.BasicUserName,httpJob.BasicPassword));
                        }
                    }

                    if (!jobagentServerList.Any()) return;

                    foreach (var jobagent in jobagentServerList)
                    {
                        new Task(async () =>
                        {
                            await SendHeartbeat(jobagent.Value.Item1, jobagent.Value.Item2, jobagent.Value.Item3);
                        }).Start();
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                mDetectionTimer.Change(1000  * 5, 1000  * 5);
            }
        }


        /// <summary>
        /// 给agent发送命令后 agent自己会上报数据到storage的
        /// </summary>
        /// <param name="url"></param>
        /// <param name="basicUserName"></param>
        /// <param name="basicPassword"></param>
        /// <returns></returns>
        private static async Task SendHeartbeat(string url,string basicUserName,string basicPassword)
        {
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
                request.Headers.Add("x-job-agent-server", uri.Host+":"+uri.Port);
                if (!string.IsNullOrEmpty(ProcessMonitor.CurrentServerId))
                {
                    request.Headers.Add("x-job-server", Convert.ToBase64String(Encoding.UTF8.GetBytes(ProcessMonitor.CurrentServerId)));
                }
                var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(3));

                await client.SendAsync(request, cancelToken.Token);

            }
            catch (Exception)
            {
                //ignore
            }

        }
    }
}
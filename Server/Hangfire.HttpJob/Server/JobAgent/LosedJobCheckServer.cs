using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hangfire.Common;
using Hangfire.Heartbeat.Server;
using Hangfire.HttpJob.Support;
using Hangfire.States;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Server.JobAgent
{
    /// <summary>
    /// 失联job检查
    /// 1. hangfire的server挂了导致滞留在processing的job
    /// 2. jobagent的server挂了导致滞留于在processing的job 
    /// </summary>
    public sealed class LosedJobCheckServer
    {
        /// <summary>
        /// 每隔5s检查一次
        /// </summary>
        private static System.Threading.Timer mDetectionTimer;

        private static readonly BackgroundJobClient backClient;

        static LosedJobCheckServer()
        {
            backClient = new BackgroundJobClient();
        }


        public static void Start()
        {
            mDetectionTimer = new System.Threading.Timer(OnVerify, null, 1000 * 5, 1000 * 5);
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
                var api = JobStorage.Current.GetMonitoringApi();
                var totalCount = api.ProcessingCount();
                if (totalCount < 1) return;
                var pageSize = 50;
                var totalPages = (totalCount - 1) / pageSize + 1;
                for (int i = 0; i < totalPages; i++)
                {
                    var list = api.ProcessingJobs(pageSize * i, pageSize);
                    using (var connection = JobStorage.Current.GetConnection())
                    {
                        foreach (var job in list)
                        {
                            if (job.Value.StartedAt != null && (DateTime.UtcNow - job.Value.StartedAt.Value).TotalMinutes < 5)
                            {
                                //给5分钟缓冲
                                continue;
                            }

                            var jobItem = job.Value.Job.Args.FirstOrDefault() as HttpJobItem;

                            //如果是起多个server的情况 检查 job.Value.ServerId 是否还存活
                            var servers = JobStorage.Current.GetMonitoringApi().Servers();
                            var targetServer = servers.FirstOrDefault(r => r.Name.Equals(job.Value.ServerId));
                            //如果server不存在了 或者 server的最后心跳已经是10分钟之前了
                            if (targetServer == null || (targetServer.Heartbeat!=null && (DateTime.UtcNow - targetServer.Heartbeat.Value).TotalMinutes>10 ))
                            {
                                //hangfire的server挂了导致滞留在processing的job
                                //转移到error里面去
                                var ex1 = new HangfireServerShutDownError();
                                backClient.ChangeState(job.Key, new FailedState(ex1));
                                if (jobItem != null) HttpJob.SendFail(job.Key, jobItem, "AgentJobFail", ex1);
                                continue;
                            }


                            //查看是否是agentJob
                            if (jobItem == null || string.IsNullOrEmpty(jobItem.AgentClass)) continue;

                            //查看这个job的运行的参数是哪个agentserverId

                            var agentServerId = SerializationHelper.Deserialize<string>(connection.GetJobParameter(job.Key, "agentServerId"), SerializationOption.User);
                            if(string.IsNullOrEmpty(agentServerId))continue;

                            //查看这个agent最新的 agentserverId
                            var uri = new Uri(jobItem.Url);
                            var key = uri.Host + ":" + uri.Port;
                            var lastAgentServerHash = connection.GetAllEntriesFromHash("activeAgent:" + key);
                            if(lastAgentServerHash == null || lastAgentServerHash.Count<1) continue;
                            var lastAgentServer = lastAgentServerHash.First().Value;
                            if(string.IsNullOrEmpty(lastAgentServer))continue;
                            var currentAgentServer = SerializationHelper.Deserialize<AgentServerInfo>(lastAgentServer);
                            if(currentAgentServer==null)continue;

                            if (!string.IsNullOrEmpty(currentAgentServer.Id) && currentAgentServer.Id == agentServerId)
                            {
                                //虽然没有变化 但是agent其实已经挂了 （其实已经最少挂了15分钟了）
                                if (currentAgentServer.Time != DateTime.MinValue && (DateTime.UtcNow - currentAgentServer.Time).TotalMinutes > 10)
                                {
                                    var ex2 = new HangfireAgentShutDownError();
                                    backClient.ChangeState(job.Key, new FailedState(ex2));
                                    HttpJob.SendFail(job.Key, jobItem, "AgentJobFail", ex2);
                                }
                                continue;
                            }
                            
                            //说明这个agent已经挂了 /说明这个agent重启了
                            var ex = new HangfireAgentShutDownError();
                            backClient.ChangeState(job.Key, new FailedState(ex));
                            HttpJob.SendFail(job.Key, jobItem, "AgentJobFail", ex);
                        }
                    }
                        
                }
            }
            catch
            {
                //ignore
            }
            finally
            {
                mDetectionTimer.Change(1000 * 5, 1000 * 5);
            }
        }
    }

    class AgentServerInfo
    {
        public string Id { get; set; }
        public DateTime Time { get; set; }
    }
}

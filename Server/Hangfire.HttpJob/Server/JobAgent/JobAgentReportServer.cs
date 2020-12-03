using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Hangfire.Common;
using Hangfire.HttpJob.Support;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.HttpJob.Server.JobAgent
{
    /// <summary>
    /// 处理jobagent通过storage上报的消息
    /// </summary>
    public sealed class JobAgentReportServer
    {
        private static readonly ILog Logger = LogProvider.For<JobAgentReportServer>();
        /// <summary>
        /// 每隔2s获取一次
        /// </summary>
        private static  System.Threading.Timer mDetectionTimer ;

        private static string keyPrefix = "_agent_result_";

        private static readonly BackgroundJobClient backgroundJobClient;

        static JobAgentReportServer()
        {
            backgroundJobClient = new BackgroundJobClient();
        }

        public static void Start()
        {
            mDetectionTimer  = new System.Threading.Timer(OnVerify, null, 1000 * 2, 1000 * 2);
        }
        
        private static void OnVerify(object state)
        {
            mDetectionTimer.Change(-1, -1);
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                using (var lockStorage = connection.AcquireDistributedLock("JobAgentServer",TimeSpan.FromSeconds(30)))//防止多个server端竞争
                {
                    //拿到有上报的jobId集合
                    var jobIdList = connection.GetAllItemsFromSet(keyPrefix);
                    
                    if (jobIdList == null || !jobIdList.Any()) return;

                    foreach (var jobId in jobIdList)
                    {
                        
                        JobData jobData = connection.GetJobData(jobId);

                        //拿到真正的运行结果
                        var hashKey = keyPrefix + jobId;
                        var result = connection.GetAllEntriesFromHash(hashKey);
                        using (var tran = connection.CreateWriteTransaction())
                        {
                            //job已经不存在了 就直接删除set 
                            if (jobData == null)
                            {
                                tran.AddJobState(jobId, new SucceededState(null, 0, 0));
                                tran.RemoveFromSet(keyPrefix, jobId);
                                tran.Commit();
                                continue;
                            }
                            
                            double totalMilliseconds = (DateTime.UtcNow - jobData.CreatedAt).TotalMilliseconds;
                            long latency = (long) totalMilliseconds;

                            //如果job存在 但是没有拿到hash数据 认为成功
                            if (result == null || !result.Any())
                            {
                                tran.AddJobState(jobId, new SucceededState(null, latency, latency));
                                tran.RemoveFromSet(keyPrefix, jobId);
                                tran.RemoveHash(hashKey);
                                tran.Commit();
                                continue;
                            }

                            var resultOfAgent = result.First();
                            JobAgentResult resultData = CodingUtil.FromJson<JobAgentResult>(resultOfAgent.Value);

                            //异常数据 认为成功
                            if (resultData == null)
                            {
                                tran.AddJobState(jobId, new SucceededState(null, latency, latency));
                                tran.RemoveFromSet(keyPrefix, jobId);
                                tran.RemoveHash(hashKey);
                                tran.Commit();
                                continue;
                            }

                            //jobagent实际上运行的时长
                            long.TryParse(resultOfAgent.Key, out var realTotalMilliseconds);
                            if (realTotalMilliseconds < 1) realTotalMilliseconds = latency;
                            var isSuccess = resultData.R == "ok";
                            tran.RemoveFromSet(keyPrefix, jobId);
                            tran.RemoveHash(hashKey);
                            
                            // latency 代表的是 从开始调度 到 实际结束 总共的时长
                            // realTotalMilliseconds 代表的是 jobagent开始执行 到 实际结束的 总共的时长
                            if (isSuccess)
                            {
                                var currentState = connection.GetStateData(jobId);
                                if (currentState != null && !string.IsNullOrEmpty(currentState.Name) &&
                                    currentState.Name.Equals("Failed"))
                                {
                                    tran.AddJobState(jobId, new SucceededState(null, latency, realTotalMilliseconds));
                                }
                                else
                                {
                                    backgroundJobClient.ChangeState(jobId, new SucceededState(null, latency, realTotalMilliseconds));
                                }
                            }
                            else
                            {
                                var jobItem = jobData.Job.Args.FirstOrDefault() as HttpJobItem;
                                var ex = new AgentJobException(jobItem.AgentClass, resultData.E);
                                backgroundJobClient.ChangeState(jobId, new FailedState(ex));
                                HttpJob.SendFail(jobId,jobItem,"AgentJobFail",ex);
                            }
                            
                            //如果是stop上报过来的时候 记录这个job最后的执行id 
                            if(!string.IsNullOrEmpty(resultData.Action) && resultData.Action.Equals("stop") && !string.IsNullOrEmpty(resultData.RunId))
                            {
                                var jobItem = jobData.Job.Args.FirstOrDefault() as HttpJobItem;
                                var jobKeyName =
                                    $"recurring-job:{(!string.IsNullOrEmpty(jobItem.RecurringJobIdentifier) ? jobItem.RecurringJobIdentifier : jobItem.JobName)}";
                                tran.SetRangeInHash(jobKeyName, new List<KeyValuePair<string, string>>{new KeyValuePair<string, string>("LastJobId",resultData.RunId)});
                            }
                            
                            //出错的话 需要走通用的出错流程
                            tran.Commit();
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Logger.ErrorException("agent reporter fail", e);
            }
            finally
            {
                mDetectionTimer.Change(1000 * 2, 1000 * 2);
            }
        }
    }
}
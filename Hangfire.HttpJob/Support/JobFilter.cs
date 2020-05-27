using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.HttpJob.Content.resx;
using Hangfire.HttpJob.Server;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Tags;
using Hangfire.Tags.Storage;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// 任务过滤
    /// </summary>
    public class JobFilter : JobFilterAttribute, IClientFilter, IServerFilter, IElectStateFilter, IApplyStateFilter
    {
        //超时时间
        /// <summary>
        /// 分布式锁过期时间
        /// </summary>
        private readonly int _timeoutInSeconds;
        public JobFilter(int timeoutInSeconds)
        {
            if (timeoutInSeconds < 0)
            {
                throw new ArgumentException(nameof(timeoutInSeconds));
            }
            _timeoutInSeconds = timeoutInSeconds;
        }
        private readonly ILog logger = LogProvider.For<JobFilter>();
        public void OnCreated(CreatedContext filterContext)
        {
            logger.InfoFormat(
            "[OnCreated] Job.Method.Name: `{0}` BackgroundJob.Id: `{1}`",
            filterContext.Job.Method.Name,
            filterContext.BackgroundJob?.Id);
        }

        public void OnCreating(CreatingContext filterContext)
        {
            logger.Info($"[OnCreating] Job.Method.Name:{filterContext.Job.Method.Name}");
        }

        public void OnPerformed(PerformedContext filterContext)
        {
            if (!filterContext.Items.ContainsKey("DistributedLock"))
            {
                throw new InvalidOperationException("can not found DistributedLock in filterContext");
            }

            //删除设置运行时被设置的参数
            try
            {
                if (filterContext.Items.TryGetValue("runtimeKey", out var hashKey))
                {
                    if (!filterContext.Items.ContainsKey("RetryCount"))//执行出错需要retry的时候才会有
                    {
                        //代表是运行期间没有throw 直接删除
                        filterContext.Items.Remove("runtimeKey");
                        var hashKeyStr = hashKey as string;
                        if (!string.IsNullOrEmpty(hashKeyStr))
                        {
                            using (var tran = filterContext.Connection.CreateWriteTransaction())
                            {
                                tran.RemoveHash(hashKeyStr);
                                tran.Commit();
                            }
                        }
                    }

                }
            }
            catch (Exception)
            {
                //ignore
            }

            try
            {
                if (filterContext.Items.TryGetValue("runtimeKey_dic", out var hashDic))
                {
                    if (hashDic is Dictionary<string, string> dic)
                    {
                        foreach (var item in dic)
                        {
                            filterContext.Items.Remove(item.Key);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }

            //释放系统自带的分布式锁
            var distributedLock = (IDisposable)filterContext.Items["DistributedLock"];
            distributedLock.Dispose();
        }

        public void OnPerforming(PerformingContext filterContext)
        {
            //设置新的分布式锁,分布式锁会阻止两个相同的任务并发执行，用方法名称和JobName
            var jobresource = $"{filterContext.BackgroundJob.Job.Method.Name}.{filterContext.BackgroundJob.Job.Args[1]}";
            var locktimeout = TimeSpan.FromSeconds(_timeoutInSeconds);
            try
            {
                var jobItem = filterContext.BackgroundJob.Job.Args.FirstOrDefault();
                var job = jobItem as HttpJobItem;
                var jobKey = string.Empty;
                if (job != null)
                {
                    var isCronJob = !string.IsNullOrEmpty(job.Cron);
                    jobKey = isCronJob ? job.JobName : filterContext.BackgroundJob.Id;
                    var conts = filterContext.Connection.GetAllItemsFromSet($"JobPauseOf:{jobKey}");
                    if (conts.Contains("true"))
                    {
                        filterContext.Canceled = true;//任务被暂停不执行直接跳过
                        return;
                    }

                    if (!string.IsNullOrEmpty(job.JobName) && TagsServiceStorage.Current != null) filterContext.BackgroundJob.Id.AddTags(job.JobName);
                }

                //设置运行时被设置的参数
                try
                {
                    var hashKey = CodingUtil.MD5(jobKey + ".runtime");
                    var excuteDataList = filterContext.Connection.GetAllEntriesFromHash(hashKey);
                    if ((excuteDataList == null || !excuteDataList.Any()) && !string.IsNullOrEmpty(job.AgentClass))
                    {
                        hashKey = CodingUtil.MD5(job.JobName + ".runtime");
                        excuteDataList = filterContext.Connection.GetAllEntriesFromHash(hashKey);
                    }

                    if (excuteDataList != null && excuteDataList.Any())
                    {
                        filterContext.Items.Add("runtimeKey", hashKey);
                        //一次性的数据
                        filterContext.Items.Add("runtimeKey_dic", excuteDataList);
                        foreach (var keyvalue in excuteDataList)
                        {
                            filterContext.Items.Add(keyvalue.Key, keyvalue.Value);
                        }
                    }

                }
                catch (Exception)
                {
                    //ignore
                }


                //申请分布式锁
                var distributedLock = filterContext.Connection.AcquireDistributedLock(jobresource, locktimeout);
                filterContext.Items["DistributedLock"] = distributedLock;
            }
            catch (Exception ec)
            {
                filterContext.Canceled = true;
                logger.Info($"[OnPerforming] BackgroundJob.Job.JObName:{filterContext.BackgroundJob.Job.Args[1]} AcquireDistributedLock Timeout,BackgroundJob.Id:{filterContext.BackgroundJob.Id},Exception:{ec}");
            }
        }

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var timeout = CodingUtil.HangfireHttpJobOptions.JobExpirationTimeoutDay < 1
                ? 1
                : CodingUtil.HangfireHttpJobOptions.JobExpirationTimeoutDay;
            context.JobExpirationTimeout = TimeSpan.FromDays(timeout);
        }

        public void OnStateElection(ElectStateContext context)
        {
            try
            {
                var jobItem = context.BackgroundJob.Job.Args.FirstOrDefault();
                var httpJobItem = jobItem as HttpJobItem;
                if (httpJobItem == null) return;

                var jobResult = context.GetJobParameter<string>("jobErr");//不跑出异常也能将job置成Fail
                if (!string.IsNullOrEmpty(jobResult))
                {
                    context.SetJobParameter("jobErr", string.Empty);//临时记录 拿到后就删除
                    if (httpJobItem != null && httpJobItem.DelayFromMinutes.Equals(-1))
                    {
                        context.CandidateState = new ErrorState(jobResult, Strings.MultiBackgroundJobFailToContinue);
                    }
                    else
                    {
                        context.CandidateState = new ErrorState(jobResult);
                    }


                    //                    if (httpJobItem.DelayFromMinutes == -1)
                    //                    {
                    //                        context.CandidateState = new DeletedState
                    //                        {
                    //                            Reason = "Start Continue Job"
                    //                        };
                    //                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var timeout = CodingUtil.HangfireHttpJobOptions.JobExpirationTimeoutDay < 1
                ? 1
                : CodingUtil.HangfireHttpJobOptions.JobExpirationTimeoutDay;
            context.JobExpirationTimeout = TimeSpan.FromDays(timeout);
        }

    }
}

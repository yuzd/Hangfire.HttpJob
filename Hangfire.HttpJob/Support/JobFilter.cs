using System;
using System.Linq;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.HttpJob.Server;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Tags;
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
            //释放系统自带的分布式锁
            var distributedLock = (IDisposable)filterContext.Items["DistributedLock"];
            distributedLock.Dispose();


            //删除设置运行时被设置的参数
            try
            {
                var hashKey = filterContext.GetJobParameter<string>("runtimeKey");
                if (!string.IsNullOrEmpty(hashKey))
                {
                    using (var tran = filterContext.Connection.CreateWriteTransaction())
                    {
                        tran.RemoveHash(hashKey);
                        tran.Commit();
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }

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
                if (job != null)
                {
                    var isCronJob = !string.IsNullOrEmpty(job.Cron);
                    var key = isCronJob ? job.JobName : filterContext.BackgroundJob.Id;
                    var conts = filterContext.Connection.GetAllItemsFromSet($"JobPauseOf:{key}");
                    if (conts.Contains("true"))
                    {
                        filterContext.Canceled = true;//任务被暂停不执行直接跳过
                        return;
                    }

                    if(!string.IsNullOrEmpty(job.JobName))filterContext.BackgroundJob.Id.AddTags(job.JobName);
                }

                //设置运行时被设置的参数
                try
                {
                    var hashKey = CodingUtil.MD5(filterContext.BackgroundJob.Id + ".runtime");
                    var excuteDataList = filterContext.Connection.GetAllEntriesFromHash(hashKey);
                    if (excuteDataList.Any())
                    {
                        filterContext.SetJobParameter("runtimeKey", hashKey);
                        foreach (var keyvalue in excuteDataList)
                        {
                            filterContext.SetJobParameter(keyvalue.Key, keyvalue.Value);
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
            var timeout = Server.HttpJob.HangfireHttpJobOptions.JobExpirationTimeoutDay < 1
                ? 1
                : Server.HttpJob.HangfireHttpJobOptions.JobExpirationTimeoutDay;
            context.JobExpirationTimeout = TimeSpan.FromDays(timeout);
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                logger.WarnFormat(
                    "[OnStateElection] BackgroundJob.Id `{0}` Failed，Exception `{1}`",
                    context.BackgroundJob.Id,
                    failedState.Exception.ToString());
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var timeout = Server.HttpJob.HangfireHttpJobOptions.JobExpirationTimeoutDay < 1
                ? 1
                : Server.HttpJob.HangfireHttpJobOptions.JobExpirationTimeoutDay;
            context.JobExpirationTimeout = TimeSpan.FromDays(timeout);
        }

    }
}

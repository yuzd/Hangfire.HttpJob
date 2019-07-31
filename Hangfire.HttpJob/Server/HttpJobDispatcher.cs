using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.HttpJob.Support;


namespace Hangfire.HttpJob.Server
{
    public class HttpJobDispatcher : IDashboardDispatcher
    {
        private static readonly ILog Logger = LogProvider.For<HttpJobDispatcher>();
        public Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            try
            {
                if (!"POST".Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return Task.FromResult(false);
                }

                var op = context.Request.GetQuery("op");
                if (string.IsNullOrEmpty(op))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Task.FromResult(false);
                }

                op = op.ToLower();
                if (op == "getjoblist")
                {
                    var joblist = GetRecurringJobs();
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.WriteAsync(JsonConvert.SerializeObject(joblist));
                    return Task.FromResult(true);
                }

                var jobItem = GetJobItem(context);
                if (jobItem == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Task.FromResult(false);
                }
                if (op == "getrecurringjob")
                {
                    var strdata = GetJobdata(jobItem.JobName);
                    if (!string.IsNullOrEmpty(strdata))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.WriteAsync(strdata);
                        return Task.FromResult(true);
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return Task.FromResult(false);
                    }
                }
                else if (op == "getbackgroundjobdetail")
                {
                    var jobDetail = GetBackGroundJobDetail(jobItem);
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.WriteAsync(JsonConvert.SerializeObject(jobDetail));
                    return Task.FromResult(true);
                }
                if (string.IsNullOrEmpty(jobItem.Url) || string.IsNullOrEmpty(jobItem.ContentType) || jobItem.Url.ToLower().Equals("http://"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Task.FromResult(false);
                }

                if (string.IsNullOrEmpty(jobItem.JobName))
                {
                    var jobName = context.Request.Path.Split('/').LastOrDefault() ?? string.Empty;
                    jobItem.JobName = jobName;
                }

                if (string.IsNullOrEmpty(jobItem.JobName))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Task.FromResult(false);
                }

                var result = false;
                switch (op)
                {
                    case "backgroundjob":
                        if (jobItem.DelayFromMinutes < -1)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Task.FromResult(false);
                        }
                        var jobId = AddHttpbackgroundjob(jobItem);
                        if (!string.IsNullOrEmpty(jobId))
                        {
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            context.Response.WriteAsync(jobId);
                            return Task.FromResult(false);
                        }
                        break;
                    case "recurringjob":
                        result = AddHttprecurringjob(jobItem);
                        break;
                    case "editrecurringjob":
                        result = AddHttprecurringjob(jobItem);
                        break;
                    case "deljob":
                        result = DelJob(jobItem);
                        break;
                    case "pausejob":
                        result = PauseOrRestartJob(jobItem.JobName);
                        break;
                    case "startbackgroudjob":
                        result = StartBackgroudJob(jobItem);
                        break;
                    case "stopbackgroudjob":
                        result = StopBackgroudJob(jobItem);
                        break;
                    default:
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return Task.FromResult(false);
                }

                if (result)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return Task.FromResult(true);
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Task.FromResult(false);
                }

            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.Dispatch", ex);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.FromResult(false);
            }

        }



        public HttpJobItem GetJobItem(DashboardContext _context)
        {
            try
            {
                Stream body = null;
                if (_context is AspNetCoreDashboardContext)
                {
                    var context = _context.GetHttpContext();
                    body = context.Request.Body;
                }
                else
                {
                    //兼容netframework

                    var contextType = _context.Request.GetType();
                    
                    //private readonly IOwinContext _context;
                    var owinContext = contextType.GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_context.Request);

                    if (owinContext == null)
                    {
                        Logger.Warn($"HttpJobDispatcher.GetJobItem:: get data from DashbordContext err,DashboardContext:{contextType.FullName}");
                        return null;
                    }

                    var request = owinContext.GetType().GetProperty("Request")?.GetValue(owinContext);

                    if (request == null)
                    {
                        Logger.Warn($"HttpJobDispatcher.GetJobItem:: get data from DashbordContext err,OwinContext:{owinContext.GetType().FullName}");
                        return null;
                    }

                    body = request.GetType().GetProperty("Body")?.GetValue(request) as Stream;
                    if (body == null)
                    {
                        Logger.Warn($"HttpJobDispatcher.GetJobItem:: get data from DashbordContext err,Request:{ request.GetType().FullName}");
                        return null;
                    }
                }

                if (body == null)
                {
                    return null;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    body.CopyTo(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    var sr = new StreamReader(ms);
                    var requestBody = sr.ReadToEnd();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<HttpJobItem>(requestBody);
                }

            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetJobItem", ex);
                return null;
            }
        }



        /// <summary>
        /// 添加后台作业
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        [JobHistorySaveTimeFilter(TimeSpanType.Second, 30)]
        public string AddHttpbackgroundjob(HttpJobItem jobItem)
        {
            try
            {
                // 普通作业
                // 单纯的httpjob 有设置延迟
                // 单纯的httpjob 没有设置延迟  但是不可以不设置延迟 所以就设置一个非常大的延迟 比如100年后
                // 以agent模式开发的job 有设置延迟
                // 以agent模式开发的job 没有设置延迟
                // 没有设置延迟 代表的只可以自己触发
                var queueName = !string.IsNullOrEmpty(jobItem.AgentClass) ? "JobAgent" : jobItem.QueueName;
                if (string.IsNullOrEmpty(queueName))
                {
                    queueName = "DEFAULT";
                }
                if (jobItem.DelayFromMinutes == -1) //约定
                {
                    //代表设置的是智能自己触发的延迟job
                    var jobId = BackgroundJob.Schedule(() => HttpJob.Excute(jobItem, jobItem.JobName + (!string.IsNullOrEmpty(jobItem.AgentClass) ? "| JobAgent |" : ""), "multiple", jobItem.EnableRetry, null), DateTimeOffset.Now.AddYears(100));

                    //自己触发完成后再把自己添加一遍
                    BackgroundJob.ContinueJobWith(jobId, () => AddHttpbackgroundjob(jobItem), JobContinuationOptions.OnAnyFinishedState);
                    return jobId;
                }

                if (jobItem.DelayFromMinutes == 0)
                {
                    return BackgroundJob.Enqueue(() => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null));
                }
                
                return BackgroundJob.Schedule(() => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null), TimeSpan.FromMinutes(jobItem.DelayFromMinutes));
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.AddHttpbackgroundjob", ex);
                return null;
            }
        }
        /// <summary>
        /// 执行job
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        public bool StartBackgroudJob(HttpJobItem jobItem)
        {
            try
            {
                if (string.IsNullOrEmpty(jobItem.Data)) return true;
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var hashKey = CodingUtil.MD5(jobItem.JobName + ".runtime");
                    using (var tran = connection.CreateWriteTransaction())
                    {
                        tran.SetRangeInHash(hashKey, new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("Data", jobItem.Data)
                        });
                        tran.Commit();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.StartBackgroudJob", ex);
                return false;
            }
        }
        public bool StopBackgroudJob(HttpJobItem jobItem)
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var hashKey = CodingUtil.MD5(jobItem.JobName + ".runtime");
                    using (var tran = connection.CreateWriteTransaction())
                    {
                        tran.SetRangeInHash(hashKey, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Action", "stop") });
                        tran.Commit();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.StopBackgroudJob", ex);
                return false;
            }
        }
        /// <summary>
        /// 停止或者暂停项目
        /// </summary>
        /// <param name="jobname"></param>
        /// <returns></returns>
        public bool PauseOrRestartJob(string jobname)
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    using (var tran = connection.CreateWriteTransaction())
                    {
                        var conts = connection.GetAllItemsFromSet($"JobPauseOf:{jobname}");
                        if (conts.Contains("true"))
                        {
                            tran.RemoveFromSet($"JobPauseOf:{jobname}", "true");
                            tran.AddToSet($"JobPauseOf:{jobname}", "false");
                            tran.Commit();
                        }
                        else
                        {
                            tran.RemoveFromSet($"JobPauseOf:{jobname}", "false");
                            tran.AddToSet($"JobPauseOf:{jobname}", "true");
                            tran.Commit();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.PauseOrRestartJob", ex);
                return false;
            }
        }
        /// <summary>
        /// 获取已经暂停的任务
        /// </summary>
        /// <returns></returns>
        public List<PauseRecurringJob> GetRecurringJobs()
        {
            var pauselist = new List<PauseRecurringJob>();
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var joblist = connection.GetRecurringJobs();
                    joblist.ForEach(k =>
                    {
                        var conts = connection.GetAllItemsFromSet($"JobPauseOf:{k.Id}");
                        if (conts.Contains("true"))
                        {
                            var pauseinfo = new PauseRecurringJob() { Id = k.Id };
                            pauselist.Add(pauseinfo);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetRecurringJobs", ex);
            }
            return pauselist;
        }


        /// <summary>
        /// 删除周期性job
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        private bool DelJob(HttpJobItem jobItem)
        {
            try
            {
                if (!string.IsNullOrEmpty(jobItem.Data) && jobItem.Data == "backgroundjob")
                {
                    return BackgroundJob.Delete(jobItem.JobName);
                }
               
                RecurringJob.RemoveIfExists(jobItem.JobName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// 添加周期性作业
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        public bool AddHttprecurringjob(HttpJobItem jobItem)
        {
            if (string.IsNullOrEmpty(jobItem.QueueName))
            {
                jobItem.QueueName = "DEFAULT";
            }
            else
            {
                //get queues from server
                // ReSharper disable once ReplaceWithSingleCallToFirstOrDefault
                var server = JobStorage.Current.GetMonitoringApi().Servers().Where(p => p.Queues.Count > 0).FirstOrDefault();
                // ReSharper disable once PossibleNullReferenceException
                var queues = server.Queues.ToList();
                if (!queues.Exists(p => p == jobItem.QueueName.ToLower()) || queues.Count == 0)
                {
                    Logger.Warn("HttpJobDispatcher.AddHttprecurringjob Error => HttpJobItem.QueueName not exist, Use DEFAULT extend!");
                }
            }

            var queueName = !string.IsNullOrEmpty(jobItem.AgentClass) ? "JobAgent" : jobItem.QueueName;
            if (string.IsNullOrEmpty(queueName))
            {
                queueName = "DEFAULT";
            }


            try
            {
                //支持添加一个 只能手动出发的
                if (string.IsNullOrEmpty(jobItem.Cron))
                {
                    RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null), Cron.Never, TimeZoneInfo.Local, jobItem.QueueName.ToLower());
                    return true;
                }

                RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null), jobItem.Cron, TimeZoneInfo.Local, jobItem.QueueName.ToLower());
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.AddHttprecurringjob", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取job任务
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetJobdata(string name)
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    Dictionary<string, string> dictionary = connection.GetAllEntriesFromHash("recurring-job:" + name);
                    if (dictionary == null || dictionary.Count == 0)
                    {
                        return "";
                    }

                    if (!dictionary.TryGetValue(nameof(Job), out var jobDetail))
                    {
                        return "";
                    }

                    var RecurringJob = InvocationData.DeserializePayload(jobDetail).DeserializeJob();

                    return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<RecurringJobItem>(RecurringJob.Args.FirstOrDefault()?.ToString()));
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetJobdata", ex);
            }
            return "";
        }
        /// <summary>
        /// 获取常规作业的jobAgent类型的JobInfo
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private JobDetailInfo GetBackGroundJobDetail(HttpJobItem jobItem)
        {
            var result = new JobDetailInfo();
            var jobName = string.Empty;
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    Job job = null;

                    if (string.IsNullOrEmpty(jobItem.Cron))
                    {
                        var jobData = connection.GetJobData(jobItem.JobName);
                        if (jobData == null)
                        {
                            result.Info = "GetJobDetail Error：can not found job by id:" + jobItem.JobName;
                            return result;
                        }
                        job = jobData.Job;
                    }
                    else
                    {
                        Dictionary<string, string> dictionary = connection.GetAllEntriesFromHash("recurring-job:" + jobItem.JobName);
                        if (dictionary == null || dictionary.Count == 0)
                        {
                            result.Info = "GetJobDetail Error：can not found job by id:" + jobItem.JobName;
                            return result;
                        }

                        if (!dictionary.TryGetValue(nameof(Job), out var jobDetail))
                        {
                            result.Info = "GetJobDetail Error：can not found job by id:" + jobItem.JobName;
                            return result;
                        }
                        job = InvocationData.DeserializePayload(jobDetail).DeserializeJob();
                    }

                    var jobItem2 = job.Args.FirstOrDefault();
                    var httpJobItem = jobItem2 as HttpJobItem;
                    if (httpJobItem == null)
                    {
                        result.Info = $"GetJobDetail Error：jobData can not found job by id:" + jobItem.JobName;
                        return result;
                    }

                    result.JobName = jobName = httpJobItem.JobName;
                    if (string.IsNullOrEmpty(httpJobItem.AgentClass))
                    {
                        result.Info = $"{(!string.IsNullOrEmpty(jobName) ? "【" + jobName + "】" : string.Empty)} Error：is not AgentJob! ";
                        return result;
                    }

                    var jobInfo = HttpJob.GetAgentJobDetail(httpJobItem);
                    if (string.IsNullOrEmpty(jobInfo))
                    {
                        result.Info = $"{(!string.IsNullOrEmpty(jobName) ? "【" + jobName + "】" : string.Empty)} Error：get null info! ";
                        return result;
                    }

                    jobInfo = jobInfo.Replace("\r\n", "<br/>");
                    result.Info = jobInfo;
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetBackGroundJobDetail", ex);
                result.Info = $"{(!string.IsNullOrEmpty(jobName) ? "【" + jobName + "】" : string.Empty)} GetJobDetail Error：" + ex.ToString();
                return result;
            }
        }
    }
}

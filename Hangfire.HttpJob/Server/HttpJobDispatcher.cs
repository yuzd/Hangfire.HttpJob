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
using Hangfire.States;


namespace Hangfire.HttpJob.Server
{
    public class HttpJobDispatcher : IDashboardDispatcher
    {
        private static readonly ILog Logger = LogProvider.For<HttpJobDispatcher>();
        public async Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            try
            {
                if (!"POST".Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
                }

                var op = context.Request.GetQuery("op");
                if (string.IsNullOrEmpty(op))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                op = op.ToLower();
                if (op == "getjoblist")
                {
                    var joblist = GetRecurringJobs();
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(joblist));
                    return;
                }

                var jobItem = await GetJobItem(context);
                if (jobItem == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
                if (op == "getrecurringjob")
                {
                    var strdata = GetJobdata(jobItem.JobName);
                    if (!string.IsNullOrEmpty(strdata))
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        await context.Response.WriteAsync(strdata);
                        return;
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }
                }
                else if (op == "getbackgroundjobdetail")
                {
                    var jobDetail = GetBackGroundJobDetail(jobItem);
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(jobDetail));
                    return;
                }
                if (string.IsNullOrEmpty(jobItem.Url) || string.IsNullOrEmpty(jobItem.ContentType) || jobItem.Url.ToLower().Equals("http://"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                if (string.IsNullOrEmpty(jobItem.JobName))
                {
                    var jobName = context.Request.Path.Split('/').LastOrDefault() ?? string.Empty;
                    jobItem.JobName = jobName;
                }

                if (string.IsNullOrEmpty(jobItem.JobName))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var result = false;
                switch (op)
                {
                    case "backgroundjob":
                        if (jobItem.DelayFromMinutes < -1)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return;
                        }
                        var jobId = AddHttpbackgroundjob(jobItem);
                        if (!string.IsNullOrEmpty(jobId))
                        {
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            await context.Response.WriteAsync(jobId);
                            return;
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
                        return ;
                }

                if (result)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return ;
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return;
                }

            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.Dispatch", ex);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

        }



        public async Task<HttpJobItem> GetJobItem(DashboardContext _context)
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
                    await body.CopyToAsync(ms);
                    await ms.FlushAsync();
                    ms.Seek(0, SeekOrigin.Begin);
                    var sr = new StreamReader(ms);
                    var requestBody = await sr.ReadToEndAsync();
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
        public string AddHttpbackgroundjob(HttpJobItem jobItem)
        {
            try
            {
                var queueName = !string.IsNullOrEmpty(jobItem.AgentClass) ? "JobAgent" : jobItem.QueueName;
                if (string.IsNullOrEmpty(queueName))
                {
                    queueName = EnqueuedState.DefaultQueue;
                }
                
                if (jobItem.DelayFromMinutes <= 0)
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
                    Dictionary<string, string> dictionary = connection.GetAllEntriesFromHash("recurring-job:" + jobname);
                    if (dictionary == null || dictionary.Count == 0)
                    {
                        return false;
                    }

                    if (!dictionary.TryGetValue(nameof(Job), out var jobDetail))
                    {
                        return false;
                    }

                    var RecurringJob = InvocationData.DeserializePayload(jobDetail).DeserializeJob();

                    var job = CodingUtil.FromJson<HttpJobItem>(RecurringJob.Args.FirstOrDefault()?.ToString());

                    if (job == null) return false;
                    
                    using (var tran = connection.CreateWriteTransaction())
                    {
                        //拿到所有的设置
                        var conts = connection.GetAllItemsFromSet($"JobPauseOf:{jobname}");

                        //有就先清掉
                        foreach (var pair in conts)
                        {
                            tran.RemoveFromSet($"JobPauseOf:{jobname}", pair);
                        }

                        var cron = conts.FirstOrDefault(r => r.StartsWith("Cron:"));
                        if (!string.IsNullOrEmpty(cron)) cron = cron.Replace("Cron:", "");
                        //如果包含有true 的 说明已经设置了 暂停 要把改成 启动 并且拿到 Cron 进行更新
                        if (conts.Contains("true"))
                        {
                            tran.AddToSet($"JobPauseOf:{jobname}", "false");
                            if (!string.IsNullOrEmpty(cron))
                            {
                                job.Cron = cron;
                                AddHttprecurringjob(job);
                            }
                        }
                        else
                        {
                            tran.AddToSet($"JobPauseOf:{jobname}", "true");
                            tran.AddToSet($"JobPauseOf:{jobname}","Cron:" +job.Cron);
                            job.Cron = "";
                            AddHttprecurringjob(job);
                        }

                        tran.Commit();
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
                jobItem.QueueName = EnqueuedState.DefaultQueue;
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
                    Logger.Warn($"HttpJobDispatcher.AddHttprecurringjob Error => HttpJobItem.QueueName：`{jobItem.QueueName}` not exist, Use DEFAULT extend!");
                }
            }

            var queueName = !string.IsNullOrEmpty(jobItem.AgentClass) ? "JobAgent" : jobItem.QueueName;
            if (string.IsNullOrEmpty(queueName))
            {
                queueName = EnqueuedState.DefaultQueue;
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

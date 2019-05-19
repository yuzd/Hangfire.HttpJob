using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace Hangfire.HttpJob.Server
{
    public class HttpJobDispatcher : IDashboardDispatcher
    {
        private static readonly ILog Logger = LogProvider.For<HttpJobDispatcher>();
        public HttpJobDispatcher(HangfireHttpJobOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
        }

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
                if (op.ToLower() == "getjoblist")
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
                if (op.ToLower() == "getrecurringjob")
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
                switch (op.ToLower())
                {
                    case "backgroundjob":
                        result = AddHttpbackgroundjob(jobItem);
                        break;
                    case "recurringjob":
                        if (string.IsNullOrEmpty(jobItem.Cron))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Task.FromResult(false);
                        }
                        result = AddHttprecurringjob(jobItem);
                        break;
                    case "editrecurringjob":
                        if (string.IsNullOrEmpty(jobItem.Cron))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            return Task.FromResult(false);
                        }
                        result = AddHttprecurringjob(jobItem);
                        break;
                    case "pausejob":
                        result = PauseOrRestartJob(jobItem.JobName);
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
                var context = _context.GetHttpContext();
                using (MemoryStream ms = new MemoryStream())
                {
                    context.Request.Body.CopyTo(ms);
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
                    var RecurringJob = connection.GetRecurringJobs().FirstOrDefault(p => p.Id == name);
                    if (RecurringJob != null)
                    {
                        return JsonConvert.SerializeObject(JsonConvert.DeserializeObject<RecurringJobItem>(RecurringJob.Job.Args.FirstOrDefault()?.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetJobdata", ex);
            }
            return "";
        }

        /// <summary>
        /// 添加后台作业
        /// </summary>
        /// <param name="jobItem"></param>
        /// <returns></returns>
        public bool AddHttpbackgroundjob(HttpJobItem jobItem)
        {
            try
            {
                BackgroundJob.Schedule(() => HttpJob.Excute(jobItem, jobItem.JobName, jobItem.QueueName, jobItem.EnableRetry, null), TimeSpan.FromMinutes(jobItem.DelayFromMinutes));
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.AddHttpbackgroundjob", ex);
                return false;
            }
        }
        /// <summary>
        /// 暂停或者开始任务
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
                    Logger.Error("HttpJobDispatcher.AddHttprecurringjob Error => HttpJobItem.QueueName not exist!");
                    return false;
                }
            }
            try
            {
                RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, jobItem.QueueName, jobItem.EnableRetry, null), jobItem.Cron, TimeZoneInfo.Local, jobItem.QueueName.ToLower());
                return true;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.AddHttprecurringjob", ex);
                return false;
            }
        }
    }
}

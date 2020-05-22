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
        enum OperateType
        {
            Unknown = 0,
            GetJobList = 10,
            ExportJobs = 11,
            ImportJobs = 12,

            GetRecurringJob = 20,
            GetBackgroundJobDetail = 21,

            BackgroundJob = 30,
            RecurringJob = 31,
            EditRecurringJob = 32,
            PauseJob = 33,
            StartBackgroundJob = 34,
            StopBackgroundJob = 35,
            DelJob = 36,

            GetGlobalSetting = 40,
            SaveGlobalSetting = 41,

        }
        private static readonly ILog Logger = LogProvider.For<HttpJobDispatcher>();

        bool CheckOperateType(string op, OperateType opType)
        {
            return string.Equals(op, opType.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }
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

                //操作类型
                var op = context.Request.GetQuery("op");
                if (string.IsNullOrEmpty(op))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                op = op.ToLower();

                if (CheckOperateType(op, OperateType.GetRecurringJob) || op == "getrecurringjob") // dashbord 上获取周期性job详情
                {
                    await GetRecurringJobDetail(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.GetBackgroundJobDetail) || op == "getbackgroundjobdetail")// if (op == "getbackgroundjobdetail") // dashbord 上获取Agent job详情
                {
                    await DoGetBackGroundJobDetail(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.DelJob) || op == "deljob")//(op == "deljob") // 删除job
                {
                    await DelJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.PauseJob)|| op == "pausejob") //if (op == "pausejob") // 暂停或开始job
                {
                    await DoPauseOrRestartJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.BackgroundJob)|| op == "backgroundjob") //if (op == "backgroundjob") //新增后台任务job
                {
                    await AddBackgroundjob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.RecurringJob) || ((op == "recurringjob" || op == "editrecurringjob"))) //if (op == "recurringjob" || op == "editrecurringjob") //新增周期性任务job
                {
                    await AddRecurringJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.EditRecurringJob)||((op == "recurringjob" || op == "editrecurringjob"))) //if (op == "recurringjob" || op == "editrecurringjob") //新增周期性任务job
                {
                    await AddRecurringJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.StartBackgroundJob)|| (op == "startbackgroudjob")) //if (op == "startbackgroudjob")
                {
                    await StartBackgroudJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.StopBackgroundJob)|| (op == "stopbackgroudjob")) //if (op == "stopbackgroudjob")
                {
                    await StopBackgroudJob(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.GetGlobalSetting)|| (op == "getglobalsetting")) // if (op == "getglobalsetting")
                {
                    await GetGlobalSetting(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.SaveGlobalSetting)|| (op == "saveglobalsetting")) //if (op == "saveglobalsetting")
                {
                    await SaveGlobalSetting(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.ExportJobs))
                {
                    await ExportJobsAsync(context);
                    return;
                }
                else if (CheckOperateType(op, OperateType.ImportJobs))
                {
                    await ImportJobsAsync(context);
                    return;

                }

                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.Dispatch", ex);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }
        }

        /// <summary>
        /// 保存全局配置
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task SaveGlobalSetting(DashboardContext context)
        {
            try
            {
                var content = await GetRequestBody<string>(context);
                if (string.IsNullOrEmpty(content))
                {
                    await context.Response.WriteAsync("err: json invaild");
                    return;
                }

                var jsonString = ConvertJsonString(content);
                if (string.IsNullOrEmpty(jsonString))
                {
                    await context.Response.WriteAsync($"err: invaild json !");
                    return;
                }

                File.WriteAllText(CodingUtil.HangfireHttpJobOptions.GlobalSettingJsonFilePath, jsonString);

                CodingUtil.GetGlobalAppsettings();
            }
            catch (Exception e)
            {
                await context.Response.WriteAsync("err:" + e.Message);
            }
        }

        /// <summary>
        /// 获取全局配置
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task GetGlobalSetting(DashboardContext context)
        {
            var path = CodingUtil.HangfireHttpJobOptions.GlobalSettingJsonFilePath;
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, "");//如果没有权限则会报错
                    await context.Response.WriteAsync("");
                    return;
                }

                var content = File.ReadAllText(path);
                await context.Response.WriteAsync(content);
            }
            catch (Exception e)
            {
                await context.Response.WriteAsync($"err:{nameof(HangfireHttpJobOptions.GlobalSettingJsonFilePath)}:[{path}] access error:{e.Message}");
            }
        }

        /// <summary>
        /// 新增周期性job
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task AddRecurringJob(DashboardContext context)
        {
            var jobItemRt = await GetCheckedJobItem(context);
            if (!string.IsNullOrEmpty(jobItemRt.Item2))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync(jobItemRt.Item2);
                return;
            }
            if (CodingUtil.HangfireHttpJobOptions.AddHttpJobFilter != null)
            {
                if (!CodingUtil.HangfireHttpJobOptions.AddHttpJobFilter(jobItemRt.Item1))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("HttpJobFilter return false");
                    return;
                }
            }
            var result = AddHttprecurringjob(jobItemRt.Item1);
            if (result)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }
            else
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }
        }


        /// <summary>
        /// 新增后台任务job
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task AddBackgroundjob(DashboardContext context)
        {
            var jobItemRt = await GetCheckedJobItem(context);
            if (!string.IsNullOrEmpty(jobItemRt.Item2))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync(jobItemRt.Item2);
                return;
            }
            if (CodingUtil.HangfireHttpJobOptions.AddHttpJobFilter != null)
            {
                if (!CodingUtil.HangfireHttpJobOptions.AddHttpJobFilter(jobItemRt.Item1))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("HttpJobFilter return false");
                    return;
                }
            }

            var jobItem = jobItemRt.Item1;
            if (jobItem.DelayFromMinutes < -1)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("DelayFromMinutes invaild");
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

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync("add fail");
            return;
        }

        /// <summary>
        /// 通用的检查并获取jobItem
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<(HttpJobItem, string)> GetCheckedJobItem(DashboardContext context)
        {
            var jobItem = await GetRequestBody<HttpJobItem>(context);
            if (jobItem == null)
            {
                return (null, "get job data fail");
            }

            string CheckHttpJobItem(HttpJobItem item, bool isParent)
            {
                if (string.IsNullOrEmpty(item.Url) || item.Url.ToLower().Equals("http://"))
                {
                    return ("Url invaild");
                }

                if (string.IsNullOrEmpty(item.ContentType))
                {
                    return ("ContentType invaild");
                }


                if (isParent)
                {
                    if (string.IsNullOrEmpty(item.JobName))
                    {
                        var jobName = item.Url.Split('/').LastOrDefault() ?? string.Empty;
                        item.JobName = jobName;
                    }

                    if (string.IsNullOrEmpty(item.JobName))
                    {
                        return ("JobName invaild");
                    }
                }

                return string.Empty;
            }

            var list = new List<HttpJobItem>();

            void AddAllJobItem(HttpJobItem item, List<HttpJobItem> listOut)
            {
                listOut.Add(item);
                if (item.Success != null)
                {
                    AddAllJobItem(item.Success, listOut);
                }

                if (item.Fail != null)
                {
                    AddAllJobItem(item.Fail, listOut);
                }
            }

            AddAllJobItem(jobItem, list);

            for (int i = 0; i < list.Count; i++)
            {
                var checkResult = CheckHttpJobItem(list[i], i == 0);
                if (!string.IsNullOrEmpty(checkResult))
                {
                    return (null, checkResult);
                }
            }

            return (jobItem, null);
        }


        /// <summary>
        /// 获取周期性job详情
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task GetRecurringJobDetail(DashboardContext context)
        {
            var jobItem = await GetRequestBody<HttpJobItem>(context);

            if (jobItem == null || string.IsNullOrEmpty(jobItem.JobName))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("invalid request body");
                return;
            }

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
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync($"jobName:{jobItem.JobName} not found");
                return;
            }
        }


        /// <summary>
        /// 获取jobDetail
        /// </summary>
        /// <param name="_context"></param>
        /// <returns></returns>
        public async Task<T> GetRequestBody<T>(DashboardContext _context)
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
                        return default(T);
                    }

                    var request = owinContext.GetType().GetProperty("Request")?.GetValue(owinContext);

                    if (request == null)
                    {
                        Logger.Warn($"HttpJobDispatcher.GetJobItem:: get data from DashbordContext err,OwinContext:{owinContext.GetType().FullName}");
                        return default(T);
                    }

                    body = request.GetType().GetProperty("Body")?.GetValue(request) as Stream;
                    if (body == null)
                    {
                        Logger.Warn($"HttpJobDispatcher.GetJobItem:: get data from DashbordContext err,Request:{request.GetType().FullName}");
                        return default(T);
                    }
                }

                if (body == null)
                {
                    return default(T);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    await body.CopyToAsync(ms);
                    await ms.FlushAsync();
                    ms.Seek(0, SeekOrigin.Begin);
                    var sr = new StreamReader(ms);
                    var requestBody = await sr.ReadToEndAsync();
                    if (typeof(T) == typeof(String))
                    {
                        return (T)(object)requestBody;
                    }
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(requestBody);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetJobItem", ex);
                return default(T);
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

                return BackgroundJob.Schedule(() => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null),
                    TimeSpan.FromMinutes(jobItem.DelayFromMinutes));
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
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StartBackgroudJob(DashboardContext context)
        {
            try
            {
                var jobItem = await GetRequestBody<HttpJobItem>(context);

                if (jobItem == null || string.IsNullOrEmpty(jobItem.JobName))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("invalid request body");
                    return;
                }

                if (string.IsNullOrEmpty(jobItem.Data))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return;
                }

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

                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.StartBackgroudJob", ex);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(ex.Message);
            }
        }

        /// <summary>
        /// 发出jobAgent停止命令
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task StopBackgroudJob(DashboardContext context)
        {
            try
            {
                var jobItem = await GetRequestBody<HttpJobItem>(context);

                if (jobItem == null || string.IsNullOrEmpty(jobItem.JobName))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("invalid request body");
                    return;
                }

                using (var connection = JobStorage.Current.GetConnection())
                {
                    var hashKey = CodingUtil.MD5(jobItem.JobName + ".runtime");
                    using (var tran = connection.CreateWriteTransaction())
                    {
                        tran.SetRangeInHash(hashKey, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("Action", "stop") });
                        tran.Commit();
                    }
                }

                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.StopBackgroudJob", ex);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(ex.Message);
            }
        }

        /// <summary>
        /// 停止或者暂停项目
        /// </summary>
        /// <param name="jobname"></param>
        /// <returns></returns>
        public string PauseOrRestartJob(string jobname)
        {
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    Dictionary<string, string> dictionary = connection.GetAllEntriesFromHash("recurring-job:" + jobname);
                    if (dictionary == null || dictionary.Count == 0)
                    {
                        return "not found recurring-job:" + jobname;
                    }

                    if (!dictionary.TryGetValue(nameof(Job), out var jobDetail))
                    {
                        return "not found recurring-job:" + jobname;
                    }

                    var RecurringJob = InvocationData.DeserializePayload(jobDetail).DeserializeJob();

                    var job = CodingUtil.FromJson<HttpJobItem>(RecurringJob.Args.FirstOrDefault()?.ToString());

                    if (job == null) return "fail parse recurring-job:" + jobname;

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
                            tran.AddToSet($"JobPauseOf:{jobname}", "Cron:" + job.Cron);
                            job.Cron = "";
                            AddHttprecurringjob(job);
                        }

                        tran.Commit();
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.PauseOrRestartJob", ex);
                return ex.Message;
            }
        }


        /// <summary>
        /// 暂停或开始job
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task DoPauseOrRestartJob(DashboardContext context)
        {
            var jobItem = await GetRequestBody<HttpJobItem>(context);
            if (string.IsNullOrEmpty(jobItem.JobName))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync("jobName invaild");
                return;
            }

            var result = PauseOrRestartJob(jobItem.JobName);

            if (!string.IsNullOrEmpty(result))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(result);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// 删除周期性job
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task DelJob(DashboardContext context)
        {
            try
            {
                var jobItem = await GetRequestBody<HttpJobItem>(context);
                if (string.IsNullOrEmpty(jobItem.JobName))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("jobName invaild");
                    return;
                }

                if (!string.IsNullOrEmpty(jobItem.Data) && jobItem.Data == "backgroundjob")
                {
                    //删除backgroundjob
                    var result = BackgroundJob.Delete(jobItem.JobName);
                    if (!result)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync($"remove:{jobItem.JobName} fail");
                        return;
                    }

                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    return;
                }

                //删除周期性job
                RecurringJob.RemoveIfExists(jobItem.JobName);
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync(ex.Message);
                return;
            }
        }

        /// <summary>
        /// 添加周期性作业
        /// </summary>
        /// <param name="jobItem"></param>
        /// <param name="timeZone">job 时区信息</param>
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

                // 先用每个job配置的 如果没有就用系统配置的 在没有就用Local
                TimeZoneInfo timeZone = null;
                if (!string.IsNullOrEmpty(jobItem.TimeZone))
                { 
                    timeZone = TimeZoneInfoHelper.OlsonTimeZoneToTimeZoneInfo(jobItem.TimeZone);
                }
                
                if(timeZone == null) timeZone = CodingUtil.HangfireHttpJobOptions.RecurringJobTimeZone ?? TimeZoneInfo.Local;
                if (string.IsNullOrEmpty(jobItem.Cron))
                {
                    //支持添加一个 只能手动出发的
                    RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null), Cron.Never,
                        timeZone, jobItem.QueueName.ToLower());
                    return true;
                }

                RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, queueName, jobItem.EnableRetry, null), jobItem.Cron,
                    timeZone, jobItem.QueueName.ToLower());
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
        /// 获取jobAgent类型的JobInfo
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task DoGetBackGroundJobDetail(DashboardContext context)
        {
            var jobDetail = await GetBackGroundJobDetail(context);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(JsonConvert.SerializeObject(jobDetail));
        }

        /// <summary>
        /// 获取jobAgent类型的JobInfo
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<JobDetailInfo> GetBackGroundJobDetail(DashboardContext context)
        {
            var jobItem = await GetRequestBody<HttpJobItem>(context);
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


        /// <summary>
        /// 导出所有的任务
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ExportJobsAsync(DashboardContext context)
        {
            try
            {
                var jobList = GetAllRecurringJobs();
                var jobItems = jobList.Select(m => m.Job.Args[0]).ToList();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(jobItems));
            }
            catch (Exception e)
            {
                await context.Response.WriteAsync("err:" + e.Message);
            }
        }

        /// <summary>
        /// 导入所有的任务
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task ImportJobsAsync(DashboardContext context)
        {
            try
            {
                var requestStr = GetRequestBody(context);
                var jobItems = JsonConvert.DeserializeObject<List<HttpJobItem>>(requestStr);
                foreach (var jobItem in jobItems)
                {
                    AddHttprecurringjob(jobItem);
                }
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(jobItems));
            }
            catch (Exception e)
            {
                await context.Response.WriteAsync("err:" + e.Message);
            }
        }

        /// <summary>
        /// 序列化jsonstring
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string ConvertJsonString(string str)
        {
            try
            {
                //格式化json字符串
                JsonSerializer serializer = new JsonSerializer();
                TextReader tr = new StringReader(str);
                JsonTextReader jtr = new JsonTextReader(tr);
                object obj = serializer.Deserialize(jtr);
                if (obj != null)
                {
                    StringWriter textWriter = new StringWriter();
                    JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 4,
                        IndentChar = ' '
                    };
                    serializer.Serialize(jsonWriter, obj);
                    return textWriter.ToString();
                }
                else
                {
                    return str;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }


        List<RecurringJobDto> GetAllRecurringJobs()
        {
            var jobList = new List<RecurringJobDto>();
            try
            {
                using (var connection = JobStorage.Current.GetConnection())
                {
                    jobList = connection.GetRecurringJobs();

                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJobDispatcher.GetAllRecurringJobs", ex);
            }
            return jobList;
        }

        string GetRequestBody(DashboardContext context)
        {
            using (var reader = new StreamReader(context.GetHttpContext().Request.Body))
            {
                return reader.ReadToEnd();
            }

        }

    }
}
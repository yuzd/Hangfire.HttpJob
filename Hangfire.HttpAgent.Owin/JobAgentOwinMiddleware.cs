using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.HttpAgent.Owin.Config;
using Hangfire.HttpAgent.Owin.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Owin;

namespace Hangfire.HttpAgent.Owin
{
    public class JobAgentOwinMiddleware : OwinMiddleware
    {
        private readonly ILogger<JobAgentOwinMiddleware> logger;
        private readonly IOptions<JobAgentOptions> options;
        private readonly ILoggerFactory loggerFactory;
        private readonly LazyConcurrentDictionary transitentJob;
        private readonly IServiceProvider serviceProvider;

        public JobAgentOwinMiddleware(OwinMiddleware next,
            ILogger<JobAgentOwinMiddleware> logger,
            IOptions<JobAgentOptions> options,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(next)
        {
            this.loggerFactory = loggerFactory;
            this.logger = logger;
            this.options = options;
            transitentJob = new LazyConcurrentDictionary();
            this.serviceProvider = serviceProvider;
        }


        public override async Task Invoke(IOwinContext owinContext)
        {
            Console.WriteLine("LocalIpAddress:" + owinContext.Request.LocalIpAddress);
            Console.WriteLine("RemoteIpAddress:" + owinContext.Request.RemoteIpAddress);

            

            owinContext.Response.ContentType = "text/plain";
            string message = string.Empty;
            try
            {
                if (!CheckAuth(owinContext, options))
                {
                    message = "err:basic auth invaild!";
                    logger.LogError(message);
                    return;
                }
                var agentClass = owinContext.Request.Headers["x-job-agent-class"].ToString();
                var agentAction = owinContext.Request.Headers["x-job-agent-action"].ToString();
                var jobBody = owinContext.Request.Headers["x-job-body"].ToString();
                var jobUrl = owinContext.Request.Headers["x-job-url"].ToString();
                var runJobId = owinContext.Request.Headers["x-job-id"].ToString();
                var storage = owinContext.Request.Headers["x-job-storage"].ToString();
                var serverInfo = owinContext.Request.Headers["x-job-server"].ToString();
                if (!string.IsNullOrEmpty(jobBody))//是base64的
                {
                    jobBody = Encoding.UTF8.GetString(Convert.FromBase64String(jobBody));
                }

                if (!string.IsNullOrEmpty(serverInfo))
                {
                    serverInfo = Encoding.UTF8.GetString(Convert.FromBase64String(serverInfo));
                }


                if (string.IsNullOrEmpty(agentAction))
                {
                    message = $"err:x-job-agent-action in headers can not be empty!";
                    logger.LogError(message);
                    return;
                }

                if (!agentAction.Equals("heartbeat") && string.IsNullOrEmpty(agentClass))
                {
                    message = "err:x-job-agent-class in headers can not be empty!";
                    logger.LogError(message);
                    return;
                }

                JobItem jobItem = null;
                if (!string.IsNullOrEmpty(jobBody))
                {
                    jobItem = Newtonsoft.Json.JsonConvert.DeserializeObject<JobItem>(jobBody);
                }

                if (jobItem == null) jobItem = new JobItem();

                if (!string.IsNullOrEmpty(jobUrl))//是base64的
                {
                    jobUrl = Encoding.UTF8.GetString(Convert.FromBase64String(jobUrl));
                    jobItem.JobDetailUrl = jobUrl;
                }

                //本地没有配置过 从服务端里面拿
                if (JobStorageConfig.LocalJobStorageConfig != null && string.IsNullOrEmpty(JobStorageConfig.LocalJobStorageConfig.HangfireDb) && !string.IsNullOrEmpty(storage))
                {
                    var storageStr = Encoding.UTF8.GetString(Convert.FromBase64String(storage));
                    jobItem.Storage = Newtonsoft.Json.JsonConvert.DeserializeObject<JobStorageConfig>(storageStr);
                    if (jobItem.Storage.Type != JobStorageConfig.LocalJobStorageConfig.Type)
                    {
                        message = $"err:x-job-agent-type use storage： {JobStorageConfig.LocalJobStorageConfig.Type} ，but hangfire server storage is {jobItem.Storage.Type}，please check!";
                        logger.LogError(message);
                        return;
                    }

                    if (string.IsNullOrEmpty(jobItem.Storage.HangfireDb))
                    {
                        message = $"err:x-job-agent-type use invaild storage config，please check!";
                        logger.LogError(message);
                        return;
                    }

                    if (jobItem.Storage.ExpireAtDays == null || jobItem.Storage.ExpireAtDays.Value < 1)
                        jobItem.Storage.ExpireAtDays = 7;
                }

                jobItem.JobId = runJobId;
                if (!string.IsNullOrEmpty(serverInfo)) jobItem.HangfireServerId = serverInfo.Split(new string[] { "@_@" }, StringSplitOptions.None)[0];

                agentAction = agentAction.ToLower();
                if (agentAction == "heartbeat" && !string.IsNullOrEmpty(jobItem.HangfireServerId))
                {
                    jobItem.Storage.ExpireAt = TimeSpan.FromMinutes(10);//heartbeat 只保留10分钟有效期 
                    var currentServerUrl = owinContext.Request.Headers["x-job-agent-server"].ToString();
                    var jobStorage = GetHangfireStorage(owinContext, jobItem);
                    HeartBeatReport.ReportHeartBeat(jobItem.HangfireServerId, currentServerUrl, jobStorage);
                    return;
                }

                var requestBody = await GetJobItem(owinContext);
                var agentClassType = GetAgentType(agentClass);
                var jobHeaders = GetJobHeaders(owinContext);
                if (!string.IsNullOrEmpty(agentClassType.Item2))
                {
                    message = $"err:JobClass:{agentClass} GetType err:{agentClassType.Item2}";
                    logger.LogError(message);
                    return;
                }

                if (!JobAgentServiceConfigurer.JobAgentDic.TryGetValue(agentClassType.Item1, out var metaData))
                {
                    message = $"err:JobClass:{agentClass} is not registered!";
                    logger.LogWarning(message);
                    return;
                }



                if (!metaData.Transien)
                {
                    var job = (JobAgent)serviceProvider.GetService(agentClassType.Item1);
                    if (agentAction.Equals("run"))
                    {
                        //单例的 一次只能运行一次
                        if (job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.Stopping)
                        {
                            message = $"err:JobClass:{agentClass} can not start, is already Running!";
                            logger.LogWarning(message);
                            return;
                        }
                        else if (job.JobStatus == JobStatus.Default)
                        {
                            job.Hang = metaData.Hang;
                            job.AgentClass = agentClass;
                        }

                        var jobStorage = GetHangfireStorage(owinContext, jobItem);
                        var console = GetHangfireConsole(owinContext, agentClassType.Item1, jobStorage);

                        jobItem.JobParam = requestBody;
                        job.Run(jobItem, console, jobStorage, jobHeaders);
                        message = $"JobClass:{agentClass} run success!";
                        logger.LogInformation(message);

                        return;
                    }
                    else if (agentAction.Equals("stop"))
                    {
                        if (job.JobStatus == JobStatus.Stopping)
                        {
                            message = $"err:JobClass:{agentClass} is Stopping!";
                            logger.LogWarning(message);
                            return;
                        }

                        if (job.JobStatus == JobStatus.Stoped)
                        {
                            message = $"err:JobClass:{agentClass} is already Stoped!";
                            logger.LogWarning(message);
                            return;
                        }

                        var jobStorage = GetHangfireStorage(owinContext, jobItem);
                        var console = GetHangfireConsole(owinContext, agentClassType.Item1, jobStorage);

                        job.Stop(jobItem, console, jobStorage, jobHeaders);
                        message = $"JobClass:{agentClass} stop success!";
                        logger.LogInformation(message);
                        return;
                    }
                    else if (agentAction.Equals("detail"))
                    {
                        //获取job详情
                        message = job.GetJobInfo();
                        logger.LogInformation(message);
                        return;
                    }

                    message = $"err:agentAction:{agentAction} invaild";
                    logger.LogError(message);
                    return;
                }


                if (agentAction.Equals("run"))
                {
                    var job = (JobAgent)serviceProvider.GetService(agentClassType.Item1);
                    job.Singleton = false;
                    job.AgentClass = agentClass;
                    job.Hang = metaData.Hang;
                    job.Guid = Guid.NewGuid().ToString("N");
                    job.TransitentJobDisposeEvent += transitentJob.JobRemove;
                    var jobAgentList = transitentJob.GetOrAdd(agentClass, x => new ConcurrentDictionary<string, JobAgent>());
                    jobAgentList.TryAdd(job.Guid, job);

                    var jobStorage = GetHangfireStorage(owinContext, jobItem);
                    var console = GetHangfireConsole(owinContext, agentClassType.Item1, jobStorage);
                    jobItem.JobParam = requestBody;

                    job.Run(jobItem, console, jobStorage, jobHeaders);
                    message = $"Transient JobClass:{agentClass} run success!";
                    logger.LogInformation(message);

                    return;
                }
                else if (agentAction.Equals("stop"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count < 1)
                    {
                        message = $"err:Transient JobClass:{agentClass} have no running job!";
                        logger.LogWarning(message);
                        return;
                    }
                    var instanceCount = 0;
                    var stopedJobList = new List<JobAgent>();
                    foreach (var runingJob in jobAgentList)
                    {
                        if (runingJob.Value.JobStatus == JobStatus.Stopping)
                        {
                            continue;
                        }
                        if (runingJob.Value.JobStatus == JobStatus.Stoped)
                        {
                            stopedJobList.Add(runingJob.Value);
                            continue;
                        }

                        var jobStorage = GetHangfireStorage(owinContext, jobItem);
                        var console = GetHangfireConsole(owinContext, agentClassType.Item1, jobStorage);

                        runingJob.Value.Stop(jobItem, console, jobStorage, jobHeaders);
                        instanceCount++;
                    }

                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.TryRemove(stopedJob.Guid, out _);
                    }

                    transitentJob.TryRemove(agentClass, out _);
                    message = $"JobClass:{agentClass},Instance Count:{instanceCount} stop success!";
                    logger.LogInformation(message);
                    return;
                }
                else if (agentAction.Equals("detail"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count < 1)
                    {
                        message = $"err:Transient JobClass:{agentClass} have no running job!";
                        logger.LogWarning(message);
                        return;
                    }

                    var jobInfo = new List<string>();
                    var stopedJobList = new List<JobAgent>();
                    foreach (var jobAgent in jobAgentList)
                    {
                        if (jobAgent.Value.JobStatus == JobStatus.Stoped)
                        {
                            stopedJobList.Add(jobAgent.Value);
                            continue;
                        }
                        jobInfo.Add(jobAgent.Value.GetJobInfo());
                    }
                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.TryRemove(stopedJob.Guid, out _);
                    }
                    if (jobInfo.Count < 1)
                    {
                        message = $"err:Transient JobClass:{agentClass} have no running job!";
                        logger.LogWarning(message);
                        return;
                    }
                    //获取job详情
                    message = $"Runing Instance Count:{jobInfo.Count},JobList:{string.Join("\r\n", jobInfo)}";
                    return;
                }

                message = $"err:agentAction:{agentAction} invaild";
                logger.LogError(message);

            }
            catch (Exception e)
            {
                owinContext.Response.StatusCode = 500;
                await owinContext.Response.WriteAsync(e.ToString());
            }
            finally
            {
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.StartsWith("err:"))
                    {
                        if (message.Contains("already Running") || message.Contains("already Stoped"))
                        {
                            owinContext.Response.StatusCode = 501;
                        }
                        else
                        {
                            owinContext.Response.StatusCode = 500;
                        }

                    }
                    await owinContext.Response.WriteAsync(message);
                }
            }
        }

        /// <summary>
        /// 获取Storage 通过这个媒介来处理jobagent的job的统一状态
        /// </summary>
        /// <returns></returns>
        private IHangfireStorage GetHangfireStorage(IOwinContext IOwinContext, JobItem jobItem)
        {
            if (jobItem.Storage == null) return serviceProvider.GetService<IHangfireStorage>();
            var storageFactory = serviceProvider.GetService<IStorageFactory>();
            if (storageFactory == null) return null;
            return storageFactory.CreateHangfireStorage(jobItem.Storage);
        }

        /// <summary>
        /// basi Auth检查
        /// </summary>
        /// <param name="IOwinContext"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool CheckAuth(IOwinContext IOwinContext, IOptions<JobAgentOptions> options)
        {
            var jobAgent = options.Value;
            if (jobAgent.EnabledBasicAuth && !string.IsNullOrEmpty(jobAgent.BasicUserName) && !string.IsNullOrEmpty(jobAgent.BasicUserPwd))
            {
                var request = IOwinContext.Request;
                var authHeader = request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader))
                {
                    return false;
                }
                var creds = ParseAuthHeader(authHeader);
                if (creds == null || creds.Length != 2) return false;
                if (!creds[0].Equals(jobAgent.BasicUserName) || !creds[1].Equals(jobAgent.BasicUserPwd))
                {
                    return false;
                }
            }

            return true;
        }
        private string[] ParseAuthHeader(string authHeader)
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic")) return null;

            var base64Credentials = authHeader.Substring(6);
            var credentials = Encoding.ASCII.GetString(Convert.FromBase64String(base64Credentials)).Split(new[] { ':' });

            if (credentials.Length != 2 ||
                string.IsNullOrEmpty(credentials[0]) ||
                string.IsNullOrEmpty(credentials[0])) return null;

            return credentials;
        }

        private ConcurrentDictionary<string, string> GetJobHeaders(IOwinContext context)
        {
            var result = new ConcurrentDictionary<string, string>();
            try
            {
                var agentHeader = context.Request.Headers["x-job-agent-header"]?.ToString();
                if (string.IsNullOrEmpty(agentHeader))
                {
                    return result;
                }

                var arr = agentHeader.Split(new string[] { "_@_" }, StringSplitOptions.None);
                foreach (var header in arr)
                {
                    var value = context.Request.Headers[header].ToString();
                    result.TryAdd(header, Encoding.UTF8.GetString(Convert.FromBase64String(value)));
                }
            }
            catch (Exception)
            {
                //ignore
            }
            return result;
        }

        /// <summary>
        /// 获取RequestBody
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<string> GetJobItem(IOwinContext context)
        {
            try
            {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    var requestBody = await reader.ReadToEndAsync();
                    return requestBody;
                    // Do something
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("ready body content from Request.Body err:" + e.Message);
                throw new Exception("ready body content from Request.Body err:" + e.Message);
            }
        }
        private (Type, string) GetAgentType(string agentClass)
        {
            try
            {
                var type = Type.GetType(agentClass);
                var type2 = Type.GetType("Hangfire.HttpAgent.Owin.Cons.TestJob,Hangfire.HttpAgent.Owin.Console");
                if (type == null)
                {
                    return (null, $"Type.GetType({agentClass}) = null!");
                }

                if (!typeof(JobAgent).IsAssignableFrom(type))
                {
                    return (null, $"Type:({type.FullName}) is not AssignableFrom JobAgent !");
                }


                return (type, null);
            }
            catch (Exception e)
            {
                return (null, e.Message);
            }
        }

        private IHangfireConsole GetHangfireConsole(IOwinContext IOwinContext, Type jobType, IHangfireStorage storage)
        {
            IHangfireConsole console = null;
            try
            {
                //默认每次都是有一个新的实例
                var consoleFactory = serviceProvider.GetService<IStorageFactory>();
                console = consoleFactory.CreateHangforeConsole(storage);

                ConsoleInfo consoleInfo = null;
                var agentConsole = IOwinContext.Request.Headers["x-job-agent-console"].ToString();
                if (!string.IsNullOrEmpty(agentConsole))
                {
                    consoleInfo = agentConsole.ToJson<ConsoleInfo>();
                }

                if (console != null && consoleInfo != null)
                {
                    var initConsole = console as IHangfireConsoleInit;
                    if (initConsole == null)
                    {
                        console = null;
                    }
                    else
                    {
                        initConsole.Init(consoleInfo);
                    }
                }
                else
                {
                    console = null;
                }
            }
            catch (Exception)
            {
                //ignore
            }

            if (console == null)
            {
                var jobLogger = loggerFactory.CreateLogger(jobType);
                console = new LoggerConsole(jobLogger);
            }

            return console;
        }


    }

    //public class HelloMiddleware : OwinMiddleware
    //{
    //    private readonly string message;
    //    private readonly OwinMiddleware next;
    //    public HelloMiddleware(OwinMiddleware next, string message) : base(next)
    //    {
    //        Console.WriteLine(message);
    //        this.next = next;

    //    }

    //    public override async Task Invoke(IOwinContext context)
    //    {
    //        await next.Invoke(context);
    //    }
    //}


}

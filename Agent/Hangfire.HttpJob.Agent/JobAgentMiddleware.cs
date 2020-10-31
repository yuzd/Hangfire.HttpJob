using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent.Config;
using Hangfire.HttpJob.Agent.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent
{

#if NETCORE
    internal class JobAgentMiddleware : IMiddleware
#else
using Microsoft.Owin;
public class JobAgentMiddleware : OwinMiddleware
#endif

{
        private readonly string agentServerId = Guid.NewGuid().ToString("N");
        private readonly ILogger<JobAgentMiddleware> _logger;
        private readonly IOptions<JobAgentOptions> _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly LazyConcurrentDictionary transitentJob;
#if NETCORE
        public JobAgentMiddleware(ILogger<JobAgentMiddleware> logger, IOptions<JobAgentOptions> options, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _options = options;
            transitentJob = new LazyConcurrentDictionary();
        }

#else
        private readonly IServiceProvider serviceProvider;
        public JobAgentMiddleware(OwinMiddleware next,
            ILogger<JobAgentMiddleware> logger,
            IOptions<JobAgentOptions> options,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(next)
        {
            this._loggerFactory = loggerFactory;
            this._logger = logger;
            this._options = options;
            transitentJob = new LazyConcurrentDictionary();
            this.serviceProvider = serviceProvider;
        }


#endif



#if NETCORE
        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
#else
        public override async Task Invoke(IOwinContext httpContext)
#endif

        {
            //设置当前的jobagentServerId到Header里面去
            httpContext.Response.Cookies.Append("agentServerId",agentServerId);
            httpContext.Response.Headers.Append("agentServerId", agentServerId);
            httpContext.Response.ContentType = "text/plain";
            string message = string.Empty;
            try
            {
                if (!CheckAuth(httpContext, _options))
                {
                    message = "err:basic auth invaild!";
                    _logger.LogError(message);
                    return;
                }
                var agentClass = GetHeader(httpContext, "x-job-agent-class");
                var agentAction = GetHeader(httpContext, "x-job-agent-action");
                var jobBody = GetHeader(httpContext, "x-job-body");
                var jobUrl = GetHeader(httpContext, "x-job-url");
                var runJobId = GetHeader(httpContext, "x-job-id");
                var storage = GetHeader(httpContext, "x-job-storage");
                var serverInfo = GetHeader(httpContext, "x-job-server");
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
                    _logger.LogError(message);
                    return;
                }

                if (!agentAction.Equals("heartbeat") && string.IsNullOrEmpty(agentClass))
                {
                    message = "err:x-job-agent-class in headers can not be empty!";
                    _logger.LogError(message);
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
                        _logger.LogError(message);
                        return;
                    }

                    if (string.IsNullOrEmpty(jobItem.Storage.HangfireDb))
                    {
                        message = $"err:x-job-agent-type use invaild storage config，please check!";
                        _logger.LogError(message);
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
                    var currentServerUrl = GetHeader(httpContext, "x-job-agent-server");
                    var jobStorage = GetHangfireStorage(httpContext, jobItem);
                    HeartBeatReport.ReportHeartBeat(jobItem.HangfireServerId, currentServerUrl, jobStorage);
                    return;
                }

                var requestBody = await GetJobItem(httpContext);
                var agentClassType = GetAgentType(agentClass);
                var jobHeaders = GetJobHeaders(httpContext);
                if (!string.IsNullOrEmpty(agentClassType.Item2))
                {
                    message = $"err:JobClass:{agentClass} GetType err:{agentClassType.Item2}";
                    _logger.LogError(message);
                    return;
                }

                if (!JobAgentServiceConfigurer.JobAgentDic.TryGetValue(agentClassType.Item1, out var metaData))
                {
                    message = $"err:JobClass:{agentClass} is not registered!";
                    _logger.LogWarning(message);
                    return;
                }



                if (!metaData.Transien)
                {
                    var job = (JobAgent)GetService(httpContext,agentClassType.Item1);
                    if (agentAction.Equals("run"))
                    {
                        //单例的 一次只能运行一次
                        if (job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.Stopping)
                        {
                            message = $"err:JobClass:{agentClass} can not start, is already Running!";
                            _logger.LogWarning(message);
                            return;
                        }
                        else if (job.JobStatus == JobStatus.Default)
                        {
                            job.Hang = metaData.Hang;
                            job.AgentClass = agentClass;
                        }

                        var jobStorage = GetHangfireStorage(httpContext, jobItem);
                        var console = GetHangfireConsole(httpContext, agentClassType.Item1, jobStorage);

                        jobItem.JobParam = requestBody;
                        job.Run(jobItem, console, jobStorage, jobHeaders);
                        message = $"JobClass:{agentClass} run success!";
                        _logger.LogInformation(message);

                        return;
                    }
                    else if (agentAction.Equals("stop"))
                    {
                        if (job.JobStatus == JobStatus.Stopping)
                        {
                            message = $"err:JobClass:{agentClass} is Stopping!";
                            _logger.LogWarning(message);
                            return;
                        }

                        if (job.JobStatus == JobStatus.Stoped)
                        {
                            message = $"err:JobClass:{agentClass} is already Stoped!";
                            _logger.LogWarning(message);
                            return;
                        }

                        var jobStorage = GetHangfireStorage(httpContext, jobItem);
                        var console = GetHangfireConsole(httpContext, agentClassType.Item1, jobStorage);

                        job.Stop(jobItem, console, jobStorage, jobHeaders);
                        message = $"JobClass:{agentClass} stop success!";
                        _logger.LogInformation(message);
                        return;
                    }
                    else if (agentAction.Equals("detail"))
                    {
                        //获取job详情
                        message = job.GetJobInfo();
                        _logger.LogInformation(message);
                        return;
                    }

                    message = $"err:agentAction:{agentAction} invaild";
                    _logger.LogError(message);
                    return;
                }


                if (agentAction.Equals("run"))
                {
                    var job = (JobAgent)GetService(httpContext,agentClassType.Item1);
                    job.Singleton = false;
                    job.AgentClass = agentClass;
                    job.Hang = metaData.Hang;
                    job.Guid = Guid.NewGuid().ToString("N");
                    job.TransitentJobDisposeEvent += transitentJob.JobRemove;
                    var jobAgentList = transitentJob.GetOrAdd(agentClass, x => new ConcurrentDictionary<string, JobAgent>());
                    jobAgentList.TryAdd(job.Guid, job);

                    var jobStorage = GetHangfireStorage(httpContext, jobItem);
                    var console = GetHangfireConsole(httpContext, agentClassType.Item1, jobStorage);
                    jobItem.JobParam = requestBody;

                    job.Run(jobItem, console, jobStorage, jobHeaders);
                    message = $"Transient JobClass:{agentClass} run success!";
                    _logger.LogInformation(message);

                    return;
                }
                else if (agentAction.Equals("stop"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count < 1)
                    {
                        message = $"err:Transient JobClass:{agentClass} have no running job!";
                        _logger.LogWarning(message);
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

                        var jobStorage = GetHangfireStorage(httpContext, jobItem);
                        var console = GetHangfireConsole(httpContext, agentClassType.Item1, jobStorage);

                        runingJob.Value.Stop(jobItem, console, jobStorage, jobHeaders);
                        instanceCount++;
                    }

                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.TryRemove(stopedJob.Guid, out _);
                    }

                    transitentJob.TryRemove(agentClass, out _);
                    message = $"JobClass:{agentClass},Instance Count:{instanceCount} stop success!";
                    _logger.LogInformation(message);
                    return;
                }
                else if (agentAction.Equals("detail"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count < 1)
                    {
                        message = $"err:Transient JobClass:{agentClass} have no running job!";
                        _logger.LogWarning(message);
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
                        _logger.LogWarning(message);
                        return;
                    }
                    //获取job详情
                    message = $"Runing Instance Count:{jobInfo.Count},JobList:{string.Join("\r\n", jobInfo)}";
                    return;
                }

                message = $"err:agentAction:{agentAction} invaild";
                _logger.LogError(message);

            }
            catch (Exception e)
            {
                httpContext.Response.StatusCode = 500;
                await httpContext.Response.WriteAsync(e.ToString());
            }
            finally
            {
             
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.StartsWith("err:"))
                    {
                        if (message.Contains("already Running") || message.Contains("already Stoped"))
                        {
                            httpContext.Response.StatusCode = 501;
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 500;
                        }

                    }
                    await httpContext.Response.WriteAsync(message);
                }
            }
        }

#if NETCORE
        /// <summary>
        /// 从header里面获取
        /// </summary>
        /// <returns></returns>
        private string GetHeader(HttpContext httpContext, string key)
        {
            return httpContext.Request.Headers[key].ToString();
        }

        private T GetService<T>(HttpContext httpContext)
        {
            return httpContext.RequestServices.GetService<T>();
        }
        private object GetService(HttpContext httpContext,Type type)
        {
            return httpContext.RequestServices.GetRequiredService(type);
        }
#else
        private string GetHeader(IOwinContext httpContext, string key)
        {
            return httpContext.Request.Headers[key] ?? string.Empty;
        }

        private object GetService(Type type)
        {
            return serviceProvider.GetService(type);
        }

        private object GetService(IOwinContext httpContext,Type type)
        {
            return serviceProvider.GetService(type);
        }

         private T GetService<T>(IOwinContext httpContext = null)
        {
            return serviceProvider.GetService<T>();
        }
#endif
        /// <summary>
        /// 获取Storage 通过这个媒介来处理jobagent的job的统一状态
        /// </summary>
        /// <returns></returns>
#if NETCORE
        private IHangfireStorage GetHangfireStorage(HttpContext httpContext, JobItem jobItem)
#else
         private IHangfireStorage GetHangfireStorage(IOwinContext httpContext, JobItem jobItem)
#endif
        {
            if (jobItem.Storage == null) return GetService<IHangfireStorage>(httpContext);
            var storageFactory = GetService<IStorageFactory>(httpContext);
            if (storageFactory == null) return null;
            return storageFactory.CreateHangfireStorage(jobItem.Storage);
        }


        /// <summary>
        /// basi Auth检查
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="options"></param>
        /// <returns></returns>
#if NETCORE
        private bool CheckAuth(HttpContext httpContext, IOptions<JobAgentOptions> options)
#else
        private bool CheckAuth(IOwinContext httpContext, IOptions<JobAgentOptions> options)
#endif

        {
            var jobAgent = options.Value;
            if (jobAgent.EnabledBasicAuth && !string.IsNullOrEmpty(jobAgent.BasicUserName) && !string.IsNullOrEmpty(jobAgent.BasicUserPwd))
            {
                var request = httpContext.Request;
                var authHeader = GetHeader(httpContext, "Authorization");
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

#if NETCORE
        private ConcurrentDictionary<string, string> GetJobHeaders(HttpContext context)
#else
        private ConcurrentDictionary<string, string> GetJobHeaders(IOwinContext context)
#endif

        {
            var result = new ConcurrentDictionary<string, string>();
            try
            {
                var agentHeader = GetHeader(context, "x-job-agent-header");
                if (string.IsNullOrEmpty(agentHeader))
                {
                    return result;
                }

                var arr = agentHeader.Split(new string[] { "_@_" }, StringSplitOptions.None);
                foreach (var header in arr)
                {
                    var value = GetHeader(context, header);
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
#if NETCORE
        private async Task<string> GetJobItem(HttpContext context)
#else
        private async Task<string> GetJobItem(IOwinContext context)
#endif

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
                _logger.LogWarning("ready body content from Request.Body err:" + e.Message);
                throw new Exception("ready body content from Request.Body err:" + e.Message);
            }
        }
        private (Type, string) GetAgentType(string agentClass)
        {
            try
            {
                var type = Type.GetType(agentClass);
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

#if NETCORE
        private IHangfireConsole GetHangfireConsole(HttpContext httpContext, Type jobType, IHangfireStorage storage)
#else
        private IHangfireConsole GetHangfireConsole(IOwinContext httpContext, Type jobType, IHangfireStorage storage)
#endif

        {
            IHangfireConsole console = null;
            try
            {
                //默认每次都是有一个新的实例
                var consoleFactory = GetService<IStorageFactory>(httpContext);
                console = consoleFactory.CreateHangforeConsole(storage);

                ConsoleInfo consoleInfo = null;
                var agentConsole = GetHeader(httpContext, "x-job-agent-console");
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
                var jobLogger = _loggerFactory.CreateLogger(jobType);
                console = new LoggerConsole(jobLogger);
            }

            return console;
        }
    }


}

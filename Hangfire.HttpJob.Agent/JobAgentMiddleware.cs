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
    internal class JobAgentMiddleware : IMiddleware
    {
        private readonly ILogger<JobAgentMiddleware> _logger;
        private readonly IOptions<JobAgentOptions> _options;
        private readonly ILoggerFactory _loggerFactory;
        private readonly LazyConcurrentDictionary transitentJob;
        public JobAgentMiddleware(ILogger<JobAgentMiddleware> logger, IOptions<JobAgentOptions> options, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = logger;
            _options = options;
            transitentJob = new LazyConcurrentDictionary();
        }

       
       

        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
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
                var agentClass = httpContext.Request.Headers["x-job-agent-class"].ToString();
                var agentAction = httpContext.Request.Headers["x-job-agent-action"].ToString();
                var jobBody = httpContext.Request.Headers["x-job-body"].ToString();
                if (string.IsNullOrEmpty(agentClass))
                {
                    message = "err:x-job-agent-class in headers can not be empty!";
                    _logger.LogError(message);
                    return;
                }

                if (string.IsNullOrEmpty(agentAction))
                {
                    message = $"err:x-job-agent-action in headers can not be empty!";
                    _logger.LogError(message);
                    return;
                }

                JobItem jobItem = null;
                if (!string.IsNullOrEmpty(jobBody))
                {
                    jobItem = Newtonsoft.Json.JsonConvert.DeserializeObject<JobItem>(jobBody);
                }

                if(jobItem== null)jobItem = new JobItem();

                agentAction = agentAction.ToLower();
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
                    var job = (JobAgent)httpContext.RequestServices.GetRequiredService(agentClassType.Item1);
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

                        var console = GetHangfireConsole(httpContext, agentClassType.Item1);
                        jobItem.JobParam = requestBody;
                        job.Run(jobItem, console, jobHeaders);
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
                        var console = GetHangfireConsole(httpContext, agentClassType.Item1);
                        job.Stop(jobItem,console,jobHeaders);
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
                    var job = (JobAgent)httpContext.RequestServices.GetRequiredService(agentClassType.Item1);
                    job.Singleton = false;
                    job.AgentClass = agentClass;
                    job.Hang = metaData.Hang;
                    job.Guid = Guid.NewGuid().ToString("N");
                    job.TransitentJobDisposeEvent +=  transitentJob.JobRemove;
                    var jobAgentList = transitentJob.GetOrAdd(agentClass, x => new ConcurrentDictionary<string,JobAgent>());
                    jobAgentList.TryAdd(job.Guid,job);
                    var console = GetHangfireConsole(httpContext, agentClassType.Item1);
                    jobItem.JobParam = requestBody;
                    job.Run(jobItem, console, jobHeaders);
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
                        var console = GetHangfireConsole(httpContext, agentClassType.Item1);
                        runingJob.Value.Stop(jobItem,console, jobHeaders);
                        instanceCount++;
                    }

                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.TryRemove(stopedJob.Guid,out _);
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
                        jobAgentList.TryRemove(stopedJob.Guid,out _);
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
                    if(message.StartsWith("err:")) httpContext.Response.StatusCode = 500;
                    await httpContext.Response.WriteAsync(message);
                }
            }
        }

        /// <summary>
        /// basi Auth检查
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool CheckAuth(HttpContext httpContext, IOptions<JobAgentOptions> options)
        {
            var jobAgent = options.Value;
            if (jobAgent.EnabledBasicAuth && !string.IsNullOrEmpty(jobAgent.BasicUserName) && !string.IsNullOrEmpty(jobAgent.BasicUserPwd))
            {
                var request = httpContext.Request;
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

        private ConcurrentDictionary<string,string> GetJobHeaders(HttpContext context)
        {
            var result = new ConcurrentDictionary<string,string>();
            try
            {
                var agentHeader = context.Request.Headers["x-job-agent-header"].ToString();
                if (string.IsNullOrEmpty(agentHeader))
                {
                    return result;
                }

                var arr = agentHeader.Split(new string[] {"；"}, StringSplitOptions.None);
                foreach (var header in arr)
                {
                    var value =  context.Request.Headers[header].ToString();
                    result.TryAdd(header,value);
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
        private async Task<string> GetJobItem(HttpContext context)
        {
            try
            {
                using (var reader = new StreamReader(context.Request.Body))
                {
                    var requestBody =await reader.ReadToEndAsync();
                    return requestBody;
                    // Do something
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning("ready body content from Request.Body err:"+e.Message);
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

        private IHangfireConsole GetHangfireConsole(HttpContext httpContext, Type jobType)
        {
            IHangfireConsole console = null;
            try
            {
                //默认每次都是有一个新的实例
                console = httpContext.RequestServices.GetService<IHangfireConsole>();

                ConsoleInfo consoleInfo = null;
                var agentConsole = httpContext.Request.Headers["x-job-agent-console"].ToString();
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

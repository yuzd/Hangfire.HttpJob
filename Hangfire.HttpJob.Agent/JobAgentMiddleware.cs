using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    internal class JobAgentMiddleware:IMiddleware
    {
        private readonly ILogger<JobAgentMiddleware> _logger;
        private readonly IOptions<JobAgentOptions> _options;
        public JobAgentMiddleware(ILogger<JobAgentMiddleware> logger, IOptions<JobAgentOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        private readonly LazyConcurrentDictionary<string, List<JobAgent>> transitentJob = new LazyConcurrentDictionary<string, List<JobAgent>>();

       
        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            httpContext.Response.ContentType = "text/plain";
            string message = string.Empty;
            try
            {
                if (!CheckAuth(httpContext, _options))
                {
                    message = "basic auth invaild!";
                    _logger.LogError(message);
                    return;
                }
                var agentClass = httpContext.Request.Headers["x-job-agent-class"].ToString();
                var agentAction = httpContext.Request.Headers["x-job-agent-action"].ToString();
                if (string.IsNullOrEmpty(agentClass))
                {
                    message = "x-job-agent-class in headers can not be empty!";
                    _logger.LogError(message);
                    return;
                }

                if (string.IsNullOrEmpty(agentAction))
                {
                    message = $"x-job-agent-action in headers can not be empty!";
                    _logger.LogError(message);
                    return;
                }

                agentAction = agentAction.ToLower();
                var requestBody = GetJobItem(httpContext);
                var agentClassType = GetAgentType(agentClass);
                if (!string.IsNullOrEmpty(agentClassType.Item2))
                {
                    message = $"JobClass:{agentClass} GetType err:{agentClassType.Item2}";
                    _logger.LogError(message);
                    return;
                }

                if (!JobAgentServiceConfigurer.JobAgentDic.TryGetValue(agentClassType.Item1, out var metaData))
                {
                    message = $"JobClass:{agentClass} is not registered!";
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
                            message = $"JobClass:{agentClass} can not start, is already Running!";
                            _logger.LogWarning(message);
                            return;
                        }

                        if (job.JobStatus == JobStatus.Default)
                        {
                            job.Singleton = true;
                            job.Hang = metaData.Hang;
                            job.AgentClass = agentClass;
                        }
                      
                        job.Run(requestBody);
                        message = $"JobClass:{agentClass} run success!";
                        _logger.LogInformation(message);
                        return;
                    }
                    else if (agentAction.Equals("stop"))
                    {
                        if (job.JobStatus == JobStatus.Stopping)
                        {
                            message = $"JobClass:{agentClass} is Stopping!";
                            _logger.LogWarning(message);
                            return;
                        }

                        if (job.JobStatus == JobStatus.Stoped)
                        {
                            message = $"JobClass:{agentClass} is already Stoped!";
                            _logger.LogWarning(message);
                            return;
                        }

                        job.Stop();
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

                    message = $"agentAction:{agentAction} invaild";
                    _logger.LogError(message);
                    return;
                }


                if (agentAction.Equals("run"))
                {
                    var job = (JobAgent)httpContext.RequestServices.GetRequiredService(agentClassType.Item1);
                    job.Singleton = false;
                    job.AgentClass = agentClass;
                    job.Hang = metaData.Hang;

                    var jobAgentList = transitentJob.GetOrAdd(agentClass, x => new List<JobAgent>());
                    var stopedJobList = jobAgentList.Where(r => r.JobStatus == JobStatus.Stoped).ToList();
                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.Remove(stopedJob);
                    }
                    
                    jobAgentList.Add(job);
                    job.Run(requestBody);
                    message = $"Transient JobClass:{agentClass} run success!";
                    _logger.LogInformation(message);
                   
                    return;
                }
                else if (agentAction.Equals("stop"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count<1)
                    {
                        message = $"Transient JobClass:{agentClass} have no running job!";
                        _logger.LogWarning(message);
                        return;
                    }
                    var instanceCount = 0;
                    var stopedJobList = new List<JobAgent>();
                    foreach (var runingJob in jobAgentList)
                    {
                        if (runingJob.JobStatus == JobStatus.Stopping)
                        {
                            continue;
                        }

                        if (runingJob.JobStatus == JobStatus.Stoped)
                        {
                            stopedJobList.Add(runingJob);
                            continue;
                        }
                        
                        runingJob.Stop();
                        instanceCount++;
                    }

                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.Remove(stopedJob);
                    }
                    
                    transitentJob.TryRemove(agentClass, out _);
                    message = $"JobClass:{agentClass},Instance Count:{instanceCount} stop success!";
                    _logger.LogInformation(message);
                    return;
                }
                else if (agentAction.Equals("detail"))
                {
                    if (!transitentJob.TryGetValue(agentClass, out var jobAgentList) || jobAgentList.Count<1)
                    {
                        message = $"Transient JobClass:{agentClass} have no running job!";
                        _logger.LogWarning(message);
                        return;
                    }

                    var stopedJobList = jobAgentList.Where(r => r.JobStatus == JobStatus.Stoped).ToList();
                    foreach (var stopedJob in stopedJobList)
                    {
                        jobAgentList.Remove(stopedJob);
                    }

                    var jobInfo = new List<string>();
                    foreach (var jobAgent in jobAgentList)
                    {
                        jobInfo.Add(jobAgent.GetJobInfo());
                    }

                    if (jobAgentList.Count < 1)
                    {
                        message = $"Transient JobClass:{agentClass} have no running job!";
                        _logger.LogWarning(message);
                        return;
                    }
                    //获取job详情
                    message = $"Runing Instance Count:{jobAgentList.Count},JobList:{string.Join("\r\n",jobInfo)}";
                    return;
                }

                message = $"agentAction:{agentAction} invaild";
                _logger.LogError(message);

            }
            catch (Exception e)
            {
                await httpContext.Response.WriteAsync(e.ToString());
            }
            finally
            {
                if (!string.IsNullOrEmpty(message))
                {
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
                if (creds == null || creds.Length!=2) return false;
                if (!creds[0].Equals(jobAgent.BasicUserName) ||  !creds[1].Equals(jobAgent.BasicUserPwd))
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
        /// <summary>
        /// 获取RequestBody
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string GetJobItem(HttpContext context)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    context.Request.Body.CopyTo(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    var sr = new StreamReader(ms);
                    var requestBody = sr.ReadToEnd();
                    return requestBody;
                }
            }
            catch (Exception)
            {
                return string.Empty;
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

        
    }
    
    
}

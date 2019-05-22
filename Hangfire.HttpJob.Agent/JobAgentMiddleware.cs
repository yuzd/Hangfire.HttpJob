using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.HttpJob.Agent
{
    internal class JobAgentMiddleware
    {
        private readonly ConcurrentDictionary<string, List<JobAgent>> transitentJob = new ConcurrentDictionary<string, List<JobAgent>>();
        private readonly RequestDelegate _next;
        public JobAgentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.ContentType = "text/plain";
            string message = string.Empty;
            try
            {
                lock (this)
                {
                    var agentClass = httpContext.Request.Headers["x-job-agent-class"].ToString();
                    var agentAction = httpContext.Request.Headers["x-job-agent-action"].ToString();
                    if (string.IsNullOrEmpty(agentClass))
                    {
                        message = "x-job-agent-class in headers can not be empty!";
                        return;
                    }

                    if (string.IsNullOrEmpty(agentAction))
                    {
                        message = $"x-job-agent-action in headers can not be empty!";
                        return;
                    }

                    agentAction = agentAction.ToLower();
                    var requestBody = GetJobItem(httpContext);
                    var agentClassType = GetAgentType(agentClass);
                    if (!string.IsNullOrEmpty(agentClassType.Item2))
                    {
                        message = $"JobClass:{agentClass} GetType err:{agentClassType.Item2}";
                        return;
                    }

                    if (!JobAgentServiceConfigurer.JobAgentDic.TryGetValue(agentClassType.Item1, out var metaData))
                    {
                        message = $"JobClass:{agentClass} is not registered!";
                        return;
                    }

                    if (!metaData.Transien)
                    {
                        var job = (JobAgent)httpContext.RequestServices.GetRequiredService(agentClassType.Item1);
                        if (!string.IsNullOrEmpty(job.AgentClass))
                        {
                            job.Singleton = true;
                            job.Hang = metaData.Hang;
                            job.AgentClass = agentClass;
                        }
                       
                        if (agentAction.Equals("run"))
                        {
                            //单例的 一次只能运行一次
                            if (job.JobStatus == JobStatus.Running || job.JobStatus == JobStatus.Stopping)
                            {
                                message = $"JobClass:{agentClass} is Running!";
                                return;
                            }

                            job.Run(requestBody);
                            message = $"JobClass:{agentClass} run success!";
                            return;
                        }
                        else if (agentAction.Equals("stop"))
                        {
                            if (job.JobStatus == JobStatus.Stopping)
                            {
                                message = $"JobClass:{agentClass} is Stopping!";
                                return;
                            }

                            if (job.JobStatus == JobStatus.Stoped)
                            {
                                message = $"JobClass:{agentClass} is Stoped!";
                                return;
                            }

                            job.Stop();
                            message = $"JobClass:{agentClass} stop success!";
                            return;
                        }

                        message = $"agentAction:{agentAction} invaild";
                        return;
                    }


                    if (agentAction.Equals("run"))
                    {
                        var job = (JobAgent)httpContext.RequestServices.GetRequiredService(agentClassType.Item1);
                        job.Singleton = false;
                        job.AgentClass = agentClass;
                        job.Hang = metaData.Hang;
                        if (!transitentJob.TryGetValue(agentClass, out var jobAgentList))
                        {
                            jobAgentList = new List<JobAgent> { job };
                            transitentJob.TryAdd(agentClass, jobAgentList);
                        }
                        else
                        {
                            jobAgentList.Add(job);
                        }

                        job.Run(requestBody);
                        message = $"Transient JobClass:{agentClass} run success!";
                        return;
                    }
                    else if (agentAction.Equals("stop"))
                    {
                        var instanceCount = 0;
                        if (!transitentJob.TryGetValue(agentClass, out var jobAgentList))
                        {
                            message = $"Transient JobClass:{agentClass} have no running job!";
                            return;
                        }

                        foreach (var runingJob in jobAgentList)
                        {
                            if (runingJob.JobStatus == JobStatus.Stopping || runingJob.JobStatus == JobStatus.Stoped)
                            {
                                continue;
                            }

                            runingJob.Stop();
                            instanceCount++;
                        }

                        transitentJob.TryRemove(agentClass, out _);
                        message = $"JobClass:{agentClass},Instance Count:{instanceCount} stop success!";
                        return;
                    }

                    message = $"agentAction:{agentAction} invaild";
                }

            }
            catch (Exception e)
            {
                await httpContext.Response.WriteAsync(e.ToString());
            }
            finally
            {
                if (!string.IsNullOrEmpty(message)) await httpContext.Response.WriteAsync(message);
            }
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

                if (!type.IsAssignableFrom(typeof(JobAgent)))
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

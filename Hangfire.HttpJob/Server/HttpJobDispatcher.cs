using Hangfire.Dashboard;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Server
{
    public class HttpJobDispatcher : IDashboardDispatcher
    {
        private readonly HangfireHttpJobOptions _options;

        public HttpJobDispatcher(HangfireHttpJobOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        public Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!"POST".Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            var op = context.Request.GetQuery("op");
            if (string.IsNullOrEmpty(op))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            var jobItem = GetJobItem(context);
            if (jobItem == null || string.IsNullOrEmpty(jobItem.Url) || string.IsNullOrEmpty(jobItem.ContentType) ||
                string.IsNullOrEmpty(jobItem.JobName))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(jobItem.JobName))
            {
                var jobName = context.Request.Path.Split('/').LastOrDefault() ?? string.Empty;
                jobItem.JobName = jobName;
            }

            var result = false;
            switch (op.ToLower())
            {
                case "backgroundjob":
                    result = AddHttpbackgroundjob(jobItem);
                    break;
                case "recurringjob":
                    if (string.IsNullOrEmpty(jobItem.Corn))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return Task.FromResult(false);
                    }
                    result = AddHttprecurringjob(jobItem);
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

        public HttpJobItem GetJobItem(DashboardContext _context)
        {
            try
            {
                var context = _context.GetHttpContext();
                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        context.Request.Body.CopyTo(ms);
                        ms.Flush();
                        ms.Seek(0, SeekOrigin.Begin);
                        var sr = new StreamReader(ms);
                        var requestBody = sr.ReadToEnd();
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<HttpJobItem>(requestBody);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                    }

                }
            }
            catch (Exception)
            {
                return null;
            }
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
                BackgroundJob.Schedule(() => HttpJob.Excute(jobItem, jobItem.JobName, null), TimeSpan.FromMinutes(jobItem.DelayFromMinutes));
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
            try
            {
                RecurringJob.AddOrUpdate(jobItem.JobName, () => HttpJob.Excute(jobItem, jobItem.JobName, null), jobItem.Corn, TimeZoneInfo.Local);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

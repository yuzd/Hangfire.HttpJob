using HttpClientFactory.Impl;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Client
{
    public static class HangfireJobClient
    {
        /// <summary>
        /// HttpClient Factory
        /// https://github.com/yuzd/HttpClientFactory
        /// </summary>

        internal static readonly HangfireHttpClientFactory HangfireJobHttpClientFactory = new HangfireHttpClientFactory();


        #region AddBackgroundJob

        /// <summary>
        /// AddBackgroundJobAsync
        /// </summary>
        /// <param name="hangfireServerUrl">hangfire的dashbord的根目录地址</param>
        /// <param name="backgroundJob">job参数</param>
        /// <param name="option">提交httprequest的其他配置</param>
        /// <returns></returns>
        public static Task<AddBackgroundHangfirJobResult> AddBackgroundJobAsync(string hangfireServerUrl, BackgroundJob backgroundJob, HangfireServerPostOption option = null)
        {
            return PrepareAddBackgroundHttpJobItem(hangfireServerUrl, backgroundJob, option).PostAsync<AddBackgroundHangfirJobResult>();
        }

        /// <summary>
        /// AddBackgroundJob
        /// </summary>
        /// <param name="hangfireServerUrl">hangfire的dashbord的根目录地址</param>
        /// <param name="backgroundJob">job参数</param>
        /// <param name="option">提交httprequest的其他配置</param>
        /// <returns></returns>
        public static AddBackgroundHangfirJobResult AddBackgroundJob(string hangfireServerUrl, BackgroundJob backgroundJob, HangfireServerPostOption option = null)
        {
           
            return PrepareAddBackgroundHttpJobItem(hangfireServerUrl,backgroundJob,option).Post<AddBackgroundHangfirJobResult>();
        }

        private static HttpJobItem PrepareAddBackgroundHttpJobItem(string hangfireServerUrl, BackgroundJob backgroundJob,
            HangfireServerPostOption option = null)
        {
            if (string.IsNullOrEmpty(hangfireServerUrl))
            {
                throw new ArgumentNullException(nameof(hangfireServerUrl));
            }

            if (backgroundJob == null)
            {
                throw new ArgumentNullException(nameof(backgroundJob));
            }

            if (string.IsNullOrEmpty(backgroundJob.Url))
            {
                throw new ArgumentNullException(nameof(backgroundJob.Url));
            }

            if (string.IsNullOrEmpty(backgroundJob.JobName))
            {
                throw new ArgumentNullException(nameof(backgroundJob.JobName));
            }

            if (option == null) option = new HangfireServerPostOption();
            option.HttpClient = !string.IsNullOrEmpty(option.ProxyUrl) ?
                HangfireJobHttpClientFactory.GetProxiedHttpClient(option.ProxyUrl) :
                HangfireJobHttpClientFactory.GetHttpClient(hangfireServerUrl);
            var _data = string.Empty;
            if (backgroundJob.Data != null)
            {
                if (backgroundJob.Data is string _dataStr)
                {
                    _data = _dataStr;
                }
                else
                {
                    _data = JsonConvert.SerializeObject(backgroundJob.Data);
                }
            }
            var url = hangfireServerUrl.EndsWith("/httpjob?op=backgroundjob")
                ? hangfireServerUrl
                : hangfireServerUrl + "/httpjob?op=backgroundjob";
            HttpJobItem jobItem = new HttpJobItem(url, option)
            {
                Url = backgroundJob.Url,
                Method = backgroundJob.Method,
                Data = _data,
                ContentType = backgroundJob.ContentType,
                Timeout = backgroundJob.Timeout,
                DelayFromMinutes = backgroundJob.DelayFromMinutes,
                JobName = backgroundJob.JobName,
                SendSucMail = backgroundJob.SendSucMail,
                SendFaiMail = backgroundJob.SendFaiMail,
                Mail = backgroundJob.Mail != null && backgroundJob.Mail.Any() ? string.Join(",", backgroundJob.Mail) : "",
                EnableRetry = backgroundJob.EnableRetry,
                RetryTimes = backgroundJob.RetryTimes,
                RetryDelaysInSeconds = backgroundJob.RetryDelaysInSeconds,
                BasicUserName = backgroundJob.BasicUserName,
                BasicPassword = backgroundJob.BasicPassword,
                AgentClass = backgroundJob.AgentClass,
                Headers = backgroundJob.Headers
            };
            return jobItem;
        }
        #endregion

        #region AddRecurringJob
        /// <summary>
        /// AddRecurringJobAsync
        /// </summary>
        /// <param name="hangfireServerUrl">hangfire的dashbord的根目录地址</param>
        /// <param name="recurringJob">job参数</param>
        /// <param name="option">提交httprequest的其他配置</param>
        /// <returns></returns>
        public static Task<HangfirJobResult> AddRecurringJobAsync(string hangfireServerUrl, RecurringJob recurringJob, HangfireServerPostOption option = null)
        {
            return PrepareAddRecurringHttpJobItem(hangfireServerUrl, recurringJob, option).PostAsync<HangfirJobResult>();
        }
        /// <summary>
        /// AddRecurringJob
        /// </summary>
        /// <param name="hangfireServerUrl">hangfire的dashbord的根目录地址</param>
        /// <param name="recurringJob">job参数</param>
        /// <param name="option">提交httprequest的其他配置</param>
        /// <returns></returns>
        public static HangfirJobResult AddRecurringJob(string hangfireServerUrl, RecurringJob recurringJob, HangfireServerPostOption option = null)
        {
           
            return PrepareAddRecurringHttpJobItem(hangfireServerUrl,recurringJob,option).Post<HangfirJobResult>();
        }

        private static HttpJobItem PrepareAddRecurringHttpJobItem(string hangfireServerUrl, RecurringJob recurringJob,
            HangfireServerPostOption option = null)
        {
            if (string.IsNullOrEmpty(hangfireServerUrl))
            {
                throw new ArgumentNullException(nameof(hangfireServerUrl));
            }

            if (recurringJob == null)
            {
                throw new ArgumentNullException(nameof(recurringJob));
            }

            if (string.IsNullOrEmpty(recurringJob.Url))
            {
                throw new ArgumentNullException(nameof(recurringJob.Url));
            }

            if (string.IsNullOrEmpty(recurringJob.JobName))
            {
                throw new ArgumentNullException(nameof(recurringJob.JobName));
            }

            if (string.IsNullOrEmpty(recurringJob.Cron))
            {
                throw new ArgumentNullException(nameof(recurringJob.Cron));
            }

            if (option == null) option = new HangfireServerPostOption();
            option.HttpClient = !string.IsNullOrEmpty(option.ProxyUrl) ?
                HangfireJobHttpClientFactory.GetProxiedHttpClient(option.ProxyUrl) :
                HangfireJobHttpClientFactory.GetHttpClient(hangfireServerUrl);
            var _data = string.Empty;
            if (recurringJob.Data != null)
            {
                if (recurringJob.Data is string _dataStr)
                {
                    _data = _dataStr;
                }
                else
                {
                    _data = JsonConvert.SerializeObject(recurringJob.Data);
                }
            }
            var url = hangfireServerUrl.EndsWith("/httpjob?op=recurringjob")
                ? hangfireServerUrl
                : hangfireServerUrl + "/httpjob?op=recurringjob";
            HttpJobItem jobItem = new HttpJobItem(url, option)
            {
                Url = recurringJob.Url,
                Method = recurringJob.Method,
                Data = _data,
                ContentType = recurringJob.ContentType,
                Timeout = recurringJob.Timeout,
                JobName = recurringJob.JobName,
                QueueName = recurringJob.QueueName,
                Cron = recurringJob.Cron,
                SendSucMail = recurringJob.SendSucMail,
                SendFaiMail = recurringJob.SendFaiMail,
                Mail = recurringJob.Mail != null && recurringJob.Mail.Any() ? string.Join(",", recurringJob.Mail) : "",
                EnableRetry = recurringJob.EnableRetry,
                RetryTimes = recurringJob.RetryTimes,
                RetryDelaysInSeconds = recurringJob.RetryDelaysInSeconds,
                BasicUserName = recurringJob.BasicUserName,
                BasicPassword = recurringJob.BasicPassword,
                AgentClass = recurringJob.AgentClass,
                Headers = recurringJob.Headers
            };
            return jobItem;
        }
        #endregion

        #region RemoveJob

        public static Task<HangfirJobResult> RemoveBackgroundJobAsync(string hangfireServerUrl, string jobId, HangfireServerPostOption option = null)
        {
            return PrepareRemoveHttpJobItem(hangfireServerUrl, jobId, true, option).PostAsync<HangfirJobResult>();
        }
        public static HangfirJobResult RemoveBackgroundJob(string hangfireServerUrl, string jobId, HangfireServerPostOption option = null)
        {
            return PrepareRemoveHttpJobItem(hangfireServerUrl, jobId, true, option).Post<HangfirJobResult>();
        }

        /// <summary>
        /// 删除周期性JOB
        /// </summary>
        /// <param name="hangfireServerUrl"></param>
        /// <param name="jobName"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Task<HangfirJobResult> RemoveRecurringJobAsync(string hangfireServerUrl, string jobName, HangfireServerPostOption option = null)
        {
            return PrepareRemoveHttpJobItem(hangfireServerUrl, jobName,false, option).PostAsync<HangfirJobResult>();
        }

        public static HangfirJobResult RemoveRecurringJob(string hangfireServerUrl, string jobName, HangfireServerPostOption option = null)
        {
            return PrepareRemoveHttpJobItem(hangfireServerUrl, jobName,false, option).Post<HangfirJobResult>();
        }

        private static HttpJobItem PrepareRemoveHttpJobItem(string hangfireServerUrl, string jobName, bool isBackground = false,HangfireServerPostOption option = null)
        {
            if (string.IsNullOrEmpty(hangfireServerUrl))
            {
                throw new ArgumentNullException(nameof(hangfireServerUrl));
            }
            if (string.IsNullOrEmpty(jobName))
            {
                throw new ArgumentNullException(nameof(jobName));
            }

            var recurringJob = new RecurringJob { JobName = jobName };

            if (option == null) option = new HangfireServerPostOption();
            option.HttpClient = !string.IsNullOrEmpty(option.ProxyUrl) ?
                HangfireJobHttpClientFactory.GetProxiedHttpClient(option.ProxyUrl) :
                HangfireJobHttpClientFactory.GetHttpClient(hangfireServerUrl);

            var url = hangfireServerUrl.EndsWith("/httpjob?op=deljob")
                ? hangfireServerUrl
                : hangfireServerUrl + "/httpjob?op=deljob";
            HttpJobItem jobItem = new HttpJobItem(url, option)
            {
                JobName = recurringJob.JobName,
                Url = "#",
                ContentType = "application/json",
                Data = isBackground? "backgroundjob" : ""
            };
            return jobItem;
        }

        #endregion
       
    }
}

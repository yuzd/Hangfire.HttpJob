using System;
using System.Net;
using System.Net.Http;
using HttpClientFactory.Impl;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Server
{
    public class HttpJobItem : PerHostHttpClientFactory
    {
        public HttpJobItem()
        {
            Method = "Post";
            ContentType = "application/json";
            Timeout = 20000;
            DelayFromMinutes = 15;
        }
        /// <summary>
        /// 请求Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Data { get; set; }

        public string ContentType { get; set; }

        public int Timeout { get; set; }

        public int DelayFromMinutes { get; set; }
        public string Corn { get; set; }
        public string JobName { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// 是否成功发送邮件
        /// </summary>
        public bool SendSucMail { get; set; }

        /// <summary>
        /// 是否失败发送邮件
        /// </summary>
        public bool SendFaiMail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        public string BasicUserName { get; set; }
        public string BasicPassword { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public HttpClient InitHttpClient()
        {
            return GetHttpClient(this.Url);
        }

        #region 重写HttpClientFactory

        protected override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(HttpJob.HangfireHttpJobOptions.GlobalHttpTimeOut),
            };

            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("UserAgent","Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
            return client;
        }

        protected override HttpMessageHandler CreateMessageHandler()
        {
            var handler = new HttpClientHandler();
            if (HttpJob.HangfireHttpJobOptions.Proxy == null)
            {
                handler.UseProxy = false;
            }
            else
            {
                handler.Proxy = HttpJob.HangfireHttpJobOptions.Proxy;
            }

            handler.AllowAutoRedirect = false;

            handler.AutomaticDecompression = DecompressionMethods.None;

            handler.UseCookies = false;

            return handler;
        }
        #endregion
    }

    public class RecurringJobItem 
    {
       
        /// <summary>
        /// 请求Url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Data { get; set; }

        public string ContentType { get; set; }

        public int Timeout { get; set; }

        public int DelayFromMinutes { get; set; }
        public string Corn { get; set; }
        public string JobName { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// 是否成功发送邮件
        /// </summary>
        public bool SendSucMail { get; set; }

        /// <summary>
        /// 是否失败发送邮件
        /// </summary>
        public bool SendFaiMail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        public string BasicUserName { get; set; }
        public string BasicPassword { get; set; }


    }
}

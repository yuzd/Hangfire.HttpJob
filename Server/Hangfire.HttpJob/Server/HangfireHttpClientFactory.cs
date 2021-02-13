using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using HttpClientFactory;
using HttpClientFactory.Impl;

namespace Hangfire.HttpJob.Server
{
    public class HangfireHttpClientFactory:PerHostHttpClientFactory
    {
        private readonly TimeSpan _timeOut;
        private readonly string _contentType;
        /// <summary>
        /// httpjob的请求处理
        /// </summary>
        internal static  IHttpClientFactory HttpJobInstance;
        /// <summary>
        /// 钉钉的请求发送
        /// </summary>
        internal static  IHttpClientFactory DingTalkInstance;

        /// <summary>
        /// 设置httpclientFactory来处理HttpClient请求
        /// </summary>
        /// <param name="factory"></param>
        public static void SetDefaultHttpJobInstance(IHttpClientFactory  factory = null)
        {
            HttpJobInstance = factory ?? new HangfireHttpClientFactory(TimeSpan.FromHours(1),null);//这里设置1小时 是为了取消HttpClient自带的默认超时100s的限制 会在业务逻辑里面设使用实际的Timeout
        }
        
        /// <summary>
        /// 设置httpclientFactory来处理HttpClient请求
        /// </summary>
        /// <param name="factory"></param>
        public static void SetDefaultDingTalkInstance(IHttpClientFactory  factory = null)
        {
            DingTalkInstance = factory?? new HangfireHttpClientFactory(TimeSpan.FromSeconds(60), "application/json;charset=UTF-8");
        }

      
        public HangfireHttpClientFactory(TimeSpan timeOut,string contentType)
        {
            _timeOut = timeOut;
            _contentType = contentType;
        }


        protected override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
            client.Timeout = this._timeOut;

            if (!string.IsNullOrEmpty(_contentType))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("content-type", _contentType);
            }
            return client;
        }

        protected override HttpMessageHandler CreateMessageHandler(string proxyUrl = null)
        {
            var handler = new HttpClientHandler();
            if (string.IsNullOrEmpty(proxyUrl))
            {
                handler.UseProxy = false;
            }
            else
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyUrl);
            }

            handler.AllowAutoRedirect = false;
            handler.AutomaticDecompression = DecompressionMethods.None;
            handler.UseCookies = false;
            return handler;
        }
    }
}

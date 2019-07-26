using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using HttpClientFactory.Impl;

namespace Hangfire.HttpJob.Server
{
    internal class HangfireHttpClientFactory:PerHostHttpClientFactory
    {
        internal static readonly HangfireHttpClientFactory Instance = new HangfireHttpClientFactory();

        protected override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
            client.Timeout = TimeSpan.FromHours(1);//这里设置1小时 是为了取消HttpClient自带的默认超时100s的限制 会在业务逻辑里面设使用实际的Timeout
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

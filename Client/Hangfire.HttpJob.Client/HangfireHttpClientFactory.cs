using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpClientFactory.Impl;

namespace Hangfire.HttpJob.Client
{
    internal class HangfireHttpClientFactory:PerHostHttpClientFactory
    {
        protected override HttpClient CreateHttpClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.Timeout = TimeSpan.FromHours(1);
            client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.106 Safari/537.36");
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

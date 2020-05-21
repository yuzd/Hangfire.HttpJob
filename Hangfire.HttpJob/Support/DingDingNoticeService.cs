using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Hangfire.HttpJob.Server;
using Hangfire.Logging;
using Microsoft.AspNetCore.Hosting;

namespace Hangfire.HttpJob.Support
{


    public class DingDingNoticeService
    {
        private static readonly ILog Logger = LogProvider.For<DingDingNoticeService>();
        private readonly DingTalkOption DingTalkOption;

        static DingDingNoticeService InitDingTalkOption()
        {
            return new DingDingNoticeService(Server.HttpJob.HangfireHttpJobOptions.DingTalkOption);
        }

        public static DingDingNoticeService Instance => lazyDingTalkOption().Value;

        private static Lazy<DingDingNoticeService> lazyDingTalkOption()
        {
            return new Lazy<DingDingNoticeService>(InitDingTalkOption);
        }

        public void ReSetDingTalkOption(string noticeDingToken, string dingtalkPhones, bool? dingtalkAtAll)
        {
            if (!string.IsNullOrEmpty(noticeDingToken)) SetNoticeDingToken(noticeDingToken);
            if (!string.IsNullOrEmpty(dingtalkPhones)) SetDingtalkPhones(dingtalkPhones);
            if (dingtalkAtAll.HasValue) SetDingtalkAtAll(dingtalkAtAll.Value);
        }

        private void SetNoticeDingToken(string noticeDingToken)
        {
            DingTalkOption.NoticeDingToken = noticeDingToken;
        }
        private void SetDingtalkPhones(string dingtalkPhones)
        {
            DingTalkOption.DingtalkPhones = dingtalkPhones;
        }
        private void SetDingtalkAtAll(bool dingtalkAtAll)
        {
            DingTalkOption.DingtalkAtAll = dingtalkAtAll;
        }
        private DingDingNoticeService(DingTalkOption dingTalkOption)
        {
            DingTalkOption = dingTalkOption;
        }

        public void DoNotice(string jobId, string queueName, string url, string jobName, string message, string currentDomain)
        {
            var logDetail = string.IsNullOrEmpty(currentDomain) ? $"JobId is {jobId}" : $"{currentDomain}/job/jobs/details/{jobId}";
            var content =
$@"## {jobName} 任务通知
### 任务基础属性
>#### 任务名称:{jobName}|队列名称:{queueName} 
### RequestUrl(请求地址): 
> #### {url}
### 执行情况:
>#### {message}   
### 日志详细：
>#### {logDetail}     
";
            //var content = $"### {jobName} 任务通知\n" +
            //              " #### 任务基础属性\n\n" +
            //              "> #### RequestUrl:(请求地址)\n\n" +
            //              $"> ##### {url}\n\n" +
            //              "> #### JobId:(本次运行的jobId)\n\n" +
            //              $"> ##### {jobId}\n\n" +
            //              "#### 执行情况:\n\n" +
            //              $"> ##### {message}\n\n"
            //            ;

            var title = "HttpJob任务通知";

            var atMobiles = DingTalkOption.DingtalkPhones;
            var isAtAll = DingTalkOption.DingtalkAtAll;
            var token = DingTalkOption.NoticeDingToken;

            var obj = new
            {
                msgtype = "markdown",
                markdown = new
                {
                    title,
                    text = content
                },
                at = new
                {
                    atMobiles = atMobiles,
                    isAtAll = isAtAll,
                }
            };

            var requestUri = $"https://oapi.dingtalk.com/robot/send?access_token={token}";
            //var httpClient = HangfireHttpClientFactory.Instance.GetHttpClient(requestUri);


            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {

                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
            };


            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("content-type", "application/json;charset=UTF-8");

            var res = httpClient.SendAsync(request).GetAwaiter().GetResult();
            var rlt = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // LogHelper.Debug(  $"DingtalkNotice response is {rlt}, \r\n message is {obj.ToJsonStr()} ,\r\n token is {token}");
        }

    }
}

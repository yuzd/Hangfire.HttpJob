using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using HttpClientFactory.Impl;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Server
{
    public class HttpJobItem : BaseJobItems
    {
        public HttpJobItem()
        {
            Method = "Post";
            ContentType = "application/json";
            DelayFromMinutes = 15;
        }

        /// <summary>
        /// 记录回调路径
        /// </summary>
        [JsonIgnore]
        public string CallbackRoot { get; set; }

        /// <summary>
        /// 上层job执行成功的回调
        /// </summary>
        public HttpJobItem Success { get; set; }

        /// <summary>
        /// 上层job执行失败的回调
        /// </summary>
        public HttpJobItem Fail { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class RecurringJobItem : BaseJobItems
    {
        /// <summary>
        /// 上层job执行成功的回调
        /// </summary>
        public RecurringJobChildItem Success { get; set; }

        /// <summary>
        /// 上层job执行失败的回调
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public RecurringJobChildItem Fail { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class RecurringJobChildItem
    { 
        /// <summary>
        /// 请求Url
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string Url { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string Method { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string Data { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string ContentType { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public int Timeout { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string QueueName { get; set; }

        /// <summary>
        /// 传了class就代表是agentjob
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string AgentClass { get; set; }

        /// <summary>
        /// Header
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string BasicUserName { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)] 
        public string BasicPassword { get; set; }

    }


    public class BaseJobItems
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

        /// <summary>
        /// http请求类型
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Http请求的超时时间单位是毫秒
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// -1 代表手动 0代表 立即执行 >0 代表延迟分钟数
        /// </summary>
        public int DelayFromMinutes { get; set; }
        /// <summary>
        /// 周期性job的Cron表达式
        /// </summary>
        public string Cron { get; set; }
        /// <summary>
        /// job名称
        /// </summary>
        public string JobName { get; set; }
        /// <summary>
        /// 指定的队列名称
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// 传了class就代表是agentjob
        /// </summary>
        public string AgentClass { get; set; }

        /// <summary>
        /// 是否成功发送邮件
        /// </summary>
        public bool SendSuccess { get; set; }

        /// <summary>
        /// 是否失败发送邮件
        /// </summary>
        public bool SendFail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// 失败重试区间 半角逗号隔开
        /// </summary>
        public string RetryDelaysInSeconds { get; set; }

        /// <summary>
        /// 错误尝试次数自定义
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        /// basicAuth认证
        /// </summary>
        public string BasicUserName { get; set; }
        /// <summary>
        /// basicAuth认证
        /// </summary>
        public string BasicPassword { get; set; }

        /// <summary>
        /// Header
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 判断是否成功还是失败的EL表达式
        /// </summary>
        public string CallbackEL { get; set; }

        /// <summary>
        /// 每个job运行的时区
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// 钉钉配置
        /// </summary>
        public DingTalkOption DingTalk { get; set; } 

    }
}

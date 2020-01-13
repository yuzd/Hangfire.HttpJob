using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using HttpClientFactory.Impl;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Server
{
    public class HttpJobItem 
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
        public Dictionary<string,string> Headers { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

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

        public string Cron { get; set; }
        public string JobName { get; set; }
        public string QueueName { get; set; }

        /// <summary>
        /// 传了class就代表是agentjob
        /// </summary>
        public string AgentClass { get; set; }

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

        /// <summary>
        /// 失败重试区间 半角逗号隔开
        /// </summary>
        public string RetryDelaysInSeconds { get; set; }

        /// <summary>
        /// 错误尝试次数自定义
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        /// Header
        /// </summary>
        public Dictionary<string,string> Headers { get; set; }

        public string BasicUserName { get; set; }
        public string BasicPassword { get; set; }

    }
}

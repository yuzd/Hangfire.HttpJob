using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Client
{
    
    /// <summary>
    /// 循环job
    /// </summary>
    public class RecurringJob
    {
        public RecurringJob()
        {
            Method = "Post";
            ContentType = "application/json";
            Timeout = 20000;
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
        public object Data { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 超时 毫秒
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; }

        /// <summary>
        /// JOB的名称
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// QueueName的名称 如果不配置就用默认的 DEFAULT
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// 是否成功发送通知
        /// </summary>
        public bool SendSuccess { get; set; }

        /// <summary>
        /// 是否失败发送通知
        /// </summary>
        public bool SendFail { get; set; }

        /// <summary>
        /// 指定发送邮件
        /// </summary>
        public List<string> Mail { get; set; }

        /// <summary>
        /// 开启失败重启
        /// </summary>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// 错误尝试次数自定义
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        /// 失败重试区间 半角逗号隔开
        /// </summary>
        public string RetryDelaysInSeconds { get; set; }

        /// <summary>
        /// basic 验证用户名
        /// </summary>
        public string BasicUserName { get; set; }

        /// <summary>
        /// basic 验证密码
        /// </summary>
        public string BasicPassword { get; set; }

        /// <summary>
        /// 代理设置
        /// </summary>
        public string AgentClass { get; set; }

        /// <summary>
        /// 判断是否成功还是失败的EL表达式
        /// </summary>
        public string CallbackEL { get; set; }

        /// <summary>
        /// Header
        /// </summary>
        public Dictionary<string,string> Headers { get; set; } = new Dictionary<string, string>();
        
        public HttpCallbackJob Success { get; set; }
        public HttpCallbackJob Fail { get; set; }
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

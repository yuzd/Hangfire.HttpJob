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
        /// 执行http请求的ContentType
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 执行http请求的超时时间
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 延迟多长时间执行(分钟)
        /// </summary>
        public int DelayFromMinutes { get; set; }

        /// <summary>
        /// 执行计划表达式
        /// </summary>
        public string Cron { get; set; }

        /// <summary>
        /// job名称
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// 指定basic认证的username
        /// </summary>
        public string BasicUserName { get; set; }
        /// <summary>
        /// 指定basic认证的password
        /// </summary>
        public string BasicPassword { get; set; }

        /// <summary>
        /// 自定义Tostring
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

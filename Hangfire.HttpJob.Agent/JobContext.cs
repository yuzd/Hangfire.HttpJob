using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Hangfire.HttpJob.Agent
{
    public class JobContext
    { 
        private Stopwatch _stopwatch;

        public JobContext()
        {
            StartWatch();
        }
        public JobContext(CancellationTokenSource cancelToken)
        {
            CancelToken = cancelToken;
            StartWatch();
        }
        
        #region Stopwatch

       
        
        private void StartWatch()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        internal long GetElapsedMilliseconds()
        {
            if (_stopwatch == null) return -1;
            lock (_stopwatch)
            { 
                if (_stopwatch == null) return -1;
                _stopwatch.Stop();
                var result = (long)_stopwatch.ElapsedMilliseconds;
                _stopwatch = null;
                return result;
            }
        }

        #endregion
        
        
        public string Param { get; set; }
        internal string RunJobId { get; set; }
        internal string HangfireServerId { get; set; }
        internal string ActionType { get; set; }

        public CancellationTokenSource CancelToken { get; internal set; }

        public IHangfireConsole Console { get; set; }

        public JobItem JobItem { get; internal set; }
        public ConcurrentDictionary<string,string> Headers { get; set; }

        internal IHangfireStorage HangfireStorage { get; set; }
    }

    public class JobItem
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
        public string JobParam { get; set; }

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
        /// job里面的url
        /// </summary>
        public string JobDetailUrl  { get; set; }

        /// <summary>
        /// 周期性job的唯一标识
        /// </summary>
        public string RecurringJobIdentifier { get; set; }

        /// <summary>
        /// Job运行的Id
        /// </summary>
        public string JobId { get; set; }
        
        /// <summary>
        /// Hangfire调度的serverId
        /// </summary>
        public string HangfireServerId { get; set; }

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

        /// <summary>
        /// 服务端传过来的storage配置
        /// </summary>
        internal JobStorageConfig Storage { get; set; }
    }
    public class DingTalkOption
    {
        /// <summary>
        /// 钉钉Webhook地址
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 通知是否@对应手机号的人员 , 分割
        /// </summary>
        public string AtPhones { get; set; }

        /// <summary>
        ///  通知是否@所有人
        /// </summary>
        public bool IsAtAll { get; set; }
    }

    public class JobStorageConfig
    {
        public static JobStorageConfig LocalJobStorageConfig;

        public string Type { get; set; } 
        public string TablePrefix { get; set; } 
        public string HangfireDb { get; set; }
        public int? ExpireAtDays { get; set; }
        public TimeSpan? ExpireAt { get; set; }
        public int? Db { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as JobStorageConfig;

            if (item == null)
            {
                return false;
            }

            return this.Type.Equals(item.Type) && this.TablePrefix.Equals(item.TablePrefix)&&this.HangfireDb.Equals(item.HangfireDb)&&this.Db.Equals(item.Db);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.HangfireDb != null ? this.HangfireDb.GetHashCode() : 0) ) ^ (this.TablePrefix != null ? this.TablePrefix.GetHashCode() : 0);
            }
        }
    }
}

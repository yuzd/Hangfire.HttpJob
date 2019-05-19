using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Client
{

    /// <summary>
    /// 其他额外设置
    /// </summary>
    public class HangfireServerPostOption
    {
        public HangfireServerPostOption()
        {
            TimeOut = 5000;
            ThrowException = false;
        }

        /// <summary>
        /// 请求HangfireServer的超时时间 毫秒
        /// </summary>
        public int TimeOut { get; set; }

        /// <summary>
        /// 请求HangfireServer的代理
        /// </summary>
        public IWebProxy WebProxy { get; set; }

        /// <summary>
        /// 请求HangfireServer的时候错误是否抛出
        /// </summary>
        public bool ThrowException { get; set; }

        /// <summary>
        /// 请求HangfireServer的basic 验证的用户名
        /// </summary>
        public string BasicUserName { get; set; }

        /// <summary>
        /// 请求HangfireServer的basic 验证的密码
        /// </summary>
        public string BasicPassword { get; set; }
    }
}

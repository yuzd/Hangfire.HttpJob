using Hangfire.Dashboard;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    /// Url工具类
    /// </summary>
    public static class UrlUtil
    {
        /// <summary>
        /// 获取hangfire服务器url地址（解决hangfire部署后带前缀PrefixPath，后部分url路径问题）
        /// 域名无前缀路径：http://domain.com/hangfire
        /// 域名无前缀路径：http://domain.com/{PrefixPath}/hangfire
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public static string GetCurrentHangfireUrl(this DashboardContext context)
        {
            var hangfireUrl = context.Request.PathBase;
            if (!string.IsNullOrEmpty(context.Options.PrefixPath))
            {
                hangfireUrl = context.Options.PrefixPath + context.Request.PathBase;
            }
            return hangfireUrl;
        }
    }
}

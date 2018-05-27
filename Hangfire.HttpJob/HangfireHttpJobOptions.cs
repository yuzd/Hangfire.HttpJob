using System.Net;

namespace Hangfire.HttpJob
{
    public class HangfireHttpJobOptions
    {
        public int GlobalHttpTimeOut { get; set; } = 5000;
        public string AddHttpJobButtonName { get; set; } = "新增常规作业";
        public string AddRecurringJobHttpJobButtonName { get; set; } = "新增周期性作业";
        public string CloseButtonName { get; set; } = "关闭";
        public string SubmitButtonName { get; set; } = "提交";
        public string ScheduledEndPath { get; set; } = "jobs/scheduled";
        public string RecurringEndPath { get; set; } = "/recurring";

        /// <summary>
        /// 代理设置
        /// </summary>
        public IWebProxy Proxy { get; set; }


    }
}

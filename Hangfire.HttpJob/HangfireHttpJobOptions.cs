using System;
using System.Collections.Generic;
using System.Net;
using Hangfire.HttpJob.Content.resx;
using Hangfire.States;

namespace Hangfire.HttpJob
{
    public class HangfireHttpJobOptions
    {
        /// <summary>
        /// 超时时间 毫秒
        /// </summary>
        public int GlobalHttpTimeOut { get; set; } = 5000;

        /// <summary>
        /// 默认保留7天执行记录
        /// </summary>
        public int JobExpirationTimeoutDay { get; set; } = 7;
   
        /// <summary>
        /// 设置默认的执行backgroundjob的queue名称
        /// </summary>
        public string DefaultRecurringQueueName { get; set; }

        /// <summary>
        /// 全局默认的配置文件
        /// </summary>
        public string GlobalSettingJsonFilePath { get; set; } 

        /// <summary>
        /// 设置默认的执行周期性job的queue名称
        /// </summary>
        public string DefaultBackGroundJobQueueName { get; set; } = EnqueuedState.DefaultQueue;

        /// <summary>
        /// 时区设置
        /// </summary>
        public TimeZoneInfo RecurringJobTimeZone { get; set; }

        /// <summary>
        /// 代理设置
        /// </summary>
        public string Proxy { get; set; }

        /// <summary>
        /// 邮件配置
        /// </summary>
        public MailOption MailOption { get; set; } = new MailOption();

        /// <summary>
        /// 检查HttpResponseStatusCode
        ///  如果不指定 < 400 = error
        /// </summary>
        public Func<HttpStatusCode,string, bool> CheckHttpResponseStatusCode;


        #region 按钮名称和标题等自定义展示名称

        public string AddHttpJobButtonName { get; set; } = Strings.AddHttpJobButtonName;
        public string AddRecurringJobHttpJobButtonName { get; set; } = Strings.AddRecurringJobHttpJobButtonName;
        public string CloseButtonName { get; set; } = Strings.CloseButtonName;
        public string SubmitButtonName { get; set; } = Strings.SubmitButtonName;
        public string LogOutButtonName { get; set; } = Strings.LogOutButtonName;
        public string StartBackgroudJobButtonName { get; set; } = Strings.StartBackgroudJobButtonName;
        public string StopBackgroudJobButtonName { get; set; } = Strings.StopBackgroudJobButtonName;
        public string AgentJobDeatilButton { get; set; } = Strings.AgentJobDeatilButton;

        public string SearchPlaceholder { get; set; } = Strings.SearchPlaceholder;
        public string ScheduledEndPath { get; set; } = "jobs/scheduled";
        public string RecurringEndPath { get; set; } = "/recurring";

        /// <summary>
        /// cron表达式按钮名称
        /// </summary>
        public string AddCronButtonName { get; set; } = Strings.AddCronButtonName;
        public string GobalSettingButtonName { get; set; } = Strings.GobalSettingButtonName;

        public string PauseJobButtonName { get; set; } = Strings.PauseJobButtonName;

        public string EditRecurringJobButtonName { get; set; } = Strings.EditRecurringJobButtonName;



        /// <summary>
        /// 更改Dashboard标题
        /// </summary>
        public string DashboardTitle { get; set; } = Strings.DashboardTitle;
        /// <summary>
        /// 管理面板名称
        /// </summary>
        public string DashboardName { get; set; } = Strings.DashboardName;

        /// <summary>
        /// 更改底部footer取代hangfire版本名称
        /// </summary>
        public string DashboardFooter { get; set; } = "Github";

        #endregion

    }


    public class MailOption
    {

        /// <summary>
        /// 接收者邮箱 只会发送系统错误的
        /// </summary>
        public List<string> AlertMailList { get; set; } = new List<string>();

        /// <summary>
        /// SMTP地址
        /// </summary>
        public string Server { get; set; }
        /// <summary>
        /// SMTP端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 发送者邮箱
        /// </summary>
        public string User { get; set; }

        public bool UseSsl { get; set; }
        /// <summary>
        /// 校验密码
        /// </summary>
        public string Password { get; set; }

    }
}

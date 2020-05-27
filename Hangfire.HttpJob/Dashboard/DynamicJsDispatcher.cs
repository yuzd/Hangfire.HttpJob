using Hangfire.Dashboard;
using System;
using System.Text;
using System.Threading.Tasks;
using Hangfire.HttpJob.Support;
using Hangfire.Tags.Storage;

namespace Hangfire.HttpJob.Dashboard
{
    public class DynamicJsDispatcher : IDashboardDispatcher
    {
        private readonly HangfireHttpJobOptions _options;
        public DynamicJsDispatcher(HangfireHttpJobOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Dispatch(DashboardContext context)
        {
            var builder = new StringBuilder();

            string DefaultTimeZone = CodingUtil.GetGlobalAppsetting<string>("DefaultTimeZone",null);
            bool EnableDingTalk = CodingUtil.GetGlobalAppsetting<bool>("EnableDingTalk", false);

            builder.Append(@"(function (hangFire) {")
                  .Append("hangFire.httpjobConfig =  {};")
                  .AppendFormat("hangFire.httpjobConfig.DefaultTimeZone = '{0}';", DefaultTimeZone ?? _options.DefaultTimeZone)
                  .AppendFormat("hangFire.httpjobConfig.DingtalkToken = '{0}';", _options?.DingTalkOption?.Token ?? "")
                  .AppendFormat("hangFire.httpjobConfig.DingtalkPhones = '{0}';", _options?.DingTalkOption?.AtPhones ?? "")
                  .AppendFormat("hangFire.httpjobConfig.DingtalkAtAll = '{0}';", _options?.DingTalkOption?.IsAtAll ?? false ? "true" : "false")
                  .AppendFormat("hangFire.httpjobConfig.EnableDingTalk = '{0}';", EnableDingTalk ? "true":  _options?.EnableDingTalk ?? false ? "true" : "false")
                  .AppendFormat("hangFire.httpjobConfig.AddHttpJobButtonName = '{0}';", _options.AddHttpJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.ExportJobsButtonName = '{0}';", _options.ExportJobsButtonName)
                  .AppendFormat("hangFire.httpjobConfig.ImportJobsButtonName = '{0}';", _options.ImportJobsButtonName)
                  .AppendFormat("hangFire.httpjobConfig.AddRecurringJobHttpJobButtonName = '{0}';", _options.AddRecurringJobHttpJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.AddCronButtonName = '{0}';", _options.AddCronButtonName)
                  .AppendFormat("hangFire.httpjobConfig.PauseJobButtonName = '{0}';", _options.PauseJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.EditRecurringJobButtonName = '{0}';", _options.EditRecurringJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.SearchPlaceholder = '{0}';", _options.SearchPlaceholder)

                  .AppendFormat("hangFire.httpjobConfig.DashboardTitle = '{0}';", _options.DashboardTitle)
                  .AppendFormat("hangFire.httpjobConfig.DashboardName = '{0}';", _options.DashboardName)
                  .AppendFormat("hangFire.httpjobConfig.DashboardFooter = '{0}';", _options.DashboardFooter)
                  .AppendFormat("hangFire.httpjobConfig.LogOutButtonName = '{0}';", _options.LogOutButtonName)
                  .AppendFormat("hangFire.httpjobConfig.DefaultRecurringQueueName = '{0}';", _options.DefaultRecurringQueueName)
                  .AppendFormat("hangFire.httpjobConfig.DefaultBackGroundJobQueueName = '{0}';", _options.DefaultBackGroundJobQueueName)
                  .AppendFormat("hangFire.httpjobConfig.StartBackgroudJobButtonName = '{0}';", _options.StartBackgroudJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.StopBackgroudJobButtonName = '{0}';", _options.StopBackgroudJobButtonName)
                  .AppendFormat("hangFire.httpjobConfig.AgentJobDeatilButton = '{0}';", _options.AgentJobDeatilButton)

                  .AppendFormat("hangFire.httpjobConfig.CloseButtonName = '{0}';", _options.CloseButtonName)
                  .AppendFormat("hangFire.httpjobConfig.SubmitButtonName = '{0}';", _options.SubmitButtonName)
                  .AppendFormat("hangFire.httpjobConfig.GlobalHttpTimeOut = {0};", _options.GlobalHttpTimeOut)
                  .AppendFormat("hangFire.httpjobConfig.GlobalSetButtonName = '{0}';", _options.GobalSettingButtonName)
                  .AppendFormat("hangFire.httpjobConfig.AddHttpJobUrl = '{0}/httpjob?op=backgroundjob';", context.Request.PathBase)
                  .AppendFormat("hangFire.httpjobConfig.AddCronUrl = '{0}/cron';", context.Request.PathBase)
                  .AppendFormat("hangFire.httpjobConfig.AppUrl = '{0}';", context.Request.PathBase)
                  .AppendFormat("hangFire.httpjobConfig.AddRecurringJobUrl = '{0}/httpjob?op=recurringjob';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.GetRecurringJobUrl = '{0}/httpjob?op=GetRecurringJob';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.EditRecurringJobUrl = '{0}/httpjob?op=EditRecurringJob';", context.Request.PathBase)
				   .AppendFormat("hangFire.httpjobConfig.ImportJobsUrl = '{0}/httpjob?op=ImportJobs';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.ExportJobsUrl = '{0}/httpjob?op=ExportJobs';", context.Request.PathBase)

                   .AppendFormat("hangFire.httpjobConfig.GetGlobalSettingUrl = '{0}/httpjob?op=GetGlobalSetting';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.PostGlobalSettingUrl = '{0}/httpjob?op=SaveGlobalSetting';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.PauseJobUrl = '{0}/httpjob?op=PauseJob';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.AgentJobDeatilButtonUrl = '{0}/httpjob?op=getbackgroundjobdetail';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.StartBackgroudJobUrl = '{0}/httpjob?op=StartBackgroundJob';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.StopBackgroudJobUrl = '{0}/httpjob?op=StopBackgroundJob';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.GetJobListUrl = '{0}/httpjob?op=GetJobList';", context.Request.PathBase)
                   .AppendFormat("hangFire.httpjobConfig.IsReadonly = '{0}';", context.Request.PathBase.Contains("read"))
                   .AppendFormat("hangFire.httpjobConfig.ShowTag = '{0}';", TagsServiceStorage.Current != null)
                  .AppendFormat("hangFire.httpjobConfig.NeedAddNomalHttpJobButton = location.href.indexOf('{0}') >= 0;", context.Request.PathBase.Contains("read") ? "only-read" : _options.ScheduledEndPath)
                  .AppendFormat("hangFire.httpjobConfig.NeedAddRecurringHttpJobButton = location.href.indexOf('{0}') >= 0;", context.Request.PathBase.Contains("read") ? "only-read" : _options.RecurringEndPath)
                  .AppendFormat("hangFire.httpjobConfig.NeedAddCronButton = location.href.indexOf('{0}') >= 0;;", context.Request.PathBase.Contains("read") ? "only-read" : _options.RecurringEndPath)
                  .AppendFormat("hangFire.httpjobConfig.NeedEditRecurringJobButton = location.href.indexOf('{0}') >= 0;", context.Request.PathBase.Contains("read") ? "only-read" : _options.RecurringEndPath)
                  .AppendFormat("hangFire.httpjobConfig.NeedExportJobsButton = location.href.indexOf('{0}') >= 0;", context.Request.PathBase.Contains("read") ? "only-read" : _options.RecurringEndPath)
                  .AppendFormat("hangFire.httpjobConfig.NeedImportJobsButton = location.href.indexOf('{0}') >= 0;", context.Request.PathBase.Contains("read") ? "only-read" : _options.RecurringEndPath)
                  .Append("})(window.Hangfire = window.Hangfire || {});")
                  .AppendLine();

            await context.Response.WriteAsync(builder.ToString());
        }
    }
}

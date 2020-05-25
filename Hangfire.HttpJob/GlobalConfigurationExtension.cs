using System;
using System.IO;
using Hangfire.Dashboard;
using Hangfire.HttpJob.Dashboard;
using Hangfire.HttpJob.Server;
using Hangfire.HttpJob.Support;
using System.Reflection;
using Hangfire.Common;
using Hangfire.HttpJob.Dashboard.Pages;
using Hangfire.Logging;

namespace Hangfire.HttpJob
{
    public static class GlobalConfigurationExtension
    {
        private static readonly ILog Logger = LogProvider.For<HangfireHttpJobOptions>();

        public static IGlobalConfiguration UseHangfireHttpJob(this IGlobalConfiguration config, HangfireHttpJobOptions options = null)
        {
            if (options == null) options = new HangfireHttpJobOptions();
            var assembly = typeof(HangfireHttpJobOptions).GetTypeInfo().Assembly;

            JobFilterProviders.Providers.Add(new QueueProviderFilter());

            //处理http请求
            DashboardRoutes.Routes.Add("/httpjob", new HttpJobDispatcher());
            DashboardRoutes.Routes.AddRazorPage("/cron", x => new CronJobsPage());

            var jsPath = DashboardRoutes.Routes.Contains("/js[0-9]+") ? "/js[0-9]+" : "/js[0-9]{3}";
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.jsoneditor.js"));
            DashboardRoutes.Routes.Append(jsPath, new DynamicJsDispatcher(options));
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.cron.js"));
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.httpjob.js"));
            DashboardRoutes.Routes.Append(jsPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.sweetalert2.min.js"));



            var cssPath = DashboardRoutes.Routes.Contains("/css[0-9]+") ? "/css[0-9]+" : "/css[0-9]{3}";
            DashboardRoutes.Routes.Append(cssPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.jsoneditor.css"));
            DashboardRoutes.Routes.Append(cssPath, new EmbeddedResourceDispatcher(assembly, "Hangfire.HttpJob.Content.sweetalert2.min.css"));
            DashboardRoutes.Routes.Append(cssPath, new DynamicCssDispatcher(options));

            if (options.GlobalHttpTimeOut < 2000) options.GlobalHttpTimeOut = 2000;
            if (options.CheckHttpResponseStatusCode == null)
            {
                options.CheckHttpResponseStatusCode = (code,result) => ((int)code) < 400;
            }

            if (string.IsNullOrEmpty(options.GlobalSettingJsonFilePath))
            {
                options.GlobalSettingJsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hangfire_global.json");
            }

            if (!File.Exists(options.GlobalSettingJsonFilePath))
            {
                try
                {
                    File.WriteAllText(options.GlobalSettingJsonFilePath,"");//如果没有权限则会报错
                }
                catch (Exception e)
                {
                    Logger.WarnException($"{nameof(HangfireHttpJobOptions.GlobalSettingJsonFilePath)}:[{options.GlobalSettingJsonFilePath}] access error",e);
                }
            }

            CodingUtil.HangfireHttpJobOptions = options;

            return config;
        }


    }
}

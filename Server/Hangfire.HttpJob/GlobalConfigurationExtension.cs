﻿using System;
using System.IO;
using System.Linq;
using Hangfire.Dashboard;
using Hangfire.HttpJob.Dashboard;
using Hangfire.HttpJob.Server;
using Hangfire.HttpJob.Support;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.HttpJob.Dashboard.Pages;
using Hangfire.HttpJob.Server.JobAgent;
using Hangfire.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
                options.CheckHttpResponseStatusCode = (code, result) => ((int)code) < 400;
            }

            if (string.IsNullOrEmpty(options.GlobalSettingJsonFilePath))
            {
                options.GlobalSettingJsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hangfire_global.json");
            }

            if (options.GlobalHttpClientTimeOut > 0)
            {
                HangfireHttpClientFactory.SetDefaultHttpJobInstance(new HangfireHttpClientFactory(TimeSpan.FromMilliseconds(options.GlobalHttpClientTimeOut), null));
            }
            else
            {
                HangfireHttpClientFactory.SetDefaultHttpJobInstance(options.HttpJobClientFactory);
            }
            HangfireHttpClientFactory.SetDefaultDingTalkInstance(options.DingTalkClientFactory);

            CodingUtil.HangfireHttpJobOptions = options;
            JobAgentReportServer.Start();
            JobAgentHeartBeatServer.Start();
            LosedJobCheckServer.Start();
            return config;
        }


        public static IApplicationBuilder UseHangfireHttpJob(this IApplicationBuilder app)
        {
            return app.UseMiddleware<HangfireDashboardCustomOptionsMiddleware>(CodingUtil.HangfireHttpJobOptions);
        }

    }


    internal class HangfireDashboardCustomOptionsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HangfireHttpJobOptions _options;
        private readonly Regex _titleRegex = new Regex(@"\s*Hangfire\ Dashboard\s*", RegexOptions.Compiled);

        public HangfireDashboardCustomOptionsMiddleware(RequestDelegate next, HangfireHttpJobOptions options)
        {
            _next = next;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Invoke(HttpContext context)
        {

            if (!IsHtmlPageRequest(context))
            {
                await _next.Invoke(context);
                return;
            }

            var originalBody = context.Response.Body;

            using (var newBody = new MemoryStream())
            {
                context.Response.Body = newBody;

                await _next.Invoke(context);
                context.Response.Body = originalBody;

                newBody.Seek(0, SeekOrigin.Begin);

                string newContent;
                using (var reader = new StreamReader(newBody, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    newContent = await reader.ReadToEndAsync();
                }

                var newDashboardTitle = _options?.DashboardName ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(newDashboardTitle))
                {
                    newContent = _titleRegex.Replace(newContent, newDashboardTitle);
                }

                await context.Response.WriteAsync(newContent);
            }
        }

        private static bool IsHtmlPageRequest(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("Accept", out var accept)) return false;
            if (!accept.Any(a => a.IndexOf("text/html", StringComparison.OrdinalIgnoreCase) >= 0)) return false;
            return true;
        }
    }
}

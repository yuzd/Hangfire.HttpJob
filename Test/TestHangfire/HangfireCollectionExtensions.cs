using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.MySql;
using Hangfire.Tags;
using Hangfire.Tags.MySql;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace MysqlHangfire
{
    public static class HangfireCollectionExtensions
    {
        private const string HangfireSettingsKey = "Hangfire:HangfireSettings";
        private const string HttpJobOptionsKey = "Hangfire:HttpJobOptions";
        private const string HangfireConnectStringKey = "Hangfire:HangfireSettings:ConnectionString";
        private const string HangfireLangKey = "Hangfire:HttpJobOptions:Lang";

        public static IServiceCollection AddSelfHangfire(this IServiceCollection services, IConfiguration Configuration)
        {
            var hangfireSettings = Configuration.GetSection(HangfireSettingsKey);
            var httpJobOptions = Configuration.GetSection(HttpJobOptionsKey);

            services.Configure<HangfireSettings>(hangfireSettings);
            services.Configure<HangfireHttpJobOptions>(httpJobOptions);

            services.AddHangfire(globalConfiguration =>
            {
                services.ConfigurationHangfire(Configuration, globalConfiguration);
            });
            return services;
        }


        public static void ConfigurationHangfire(this IServiceCollection services, IConfiguration Configuration,
            IGlobalConfiguration globalConfiguration)
        {
            var serverProvider = services.BuildServiceProvider();
            var hangfireSettings = serverProvider.GetService<IOptions<HangfireSettings>>().Value;
            var httpJobOptions = serverProvider.GetService<IOptions<HangfireHttpJobOptions>>().Value;

            var sqlConnectStr = Configuration.GetSection(HangfireConnectStringKey).Get<string>();
            
            var mysqlOption = new MySqlStorageOptions
            {
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
                TablesPrefix = hangfireSettings.TablePrefix
            };
            
            globalConfiguration.UseStorage(new MySqlStorage(sqlConnectStr, mysqlOption))
                .UseConsole(new ConsoleOptions
                {
                    BackgroundColor = "#000079"
                })
                .UseHangfireHttpJob(httpJobOptions)
                .UseTagsWithMySql(new TagsOptions()
                {
                    TagsListStyle = TagsListStyle.Dropdown
                },sqlOptions:mysqlOption)
                .UseHeartbeatPage();
        }

        public static IApplicationBuilder ConfigureSelfHangfire(this IApplicationBuilder app, IConfiguration Configuration)
        {
            var langStr = Configuration.GetSection(HangfireLangKey).Get<string>();
            if (!string.IsNullOrEmpty(langStr))
            {
                var options = new RequestLocalizationOptions
                {
                    DefaultRequestCulture = new RequestCulture(langStr)
                };
                app.UseRequestLocalization(options);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(langStr);
            }
            
           
            
            var services = app.ApplicationServices;
            var hangfireSettings = services.GetService<IOptions<HangfireSettings>>().Value;

            var queues = hangfireSettings.JobQueues.Select(m => m.ToLower()).Distinct().ToList();

            var workerCount = Math.Max(Environment.ProcessorCount, hangfireSettings.WorkerCount); //工作线程数，当前允许的最大线程，默认20

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerName = "HangfireServer",
                ServerTimeout = TimeSpan.FromMinutes(4),
                SchedulePollingInterval = TimeSpan.FromSeconds(15), //秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
                ShutdownTimeout = TimeSpan.FromMinutes(30), //超时时间
                Queues = queues.ToArray(), //队列
                WorkerCount = workerCount
            },additionalProcesses: new[] { new ProcessMonitor() });


            var dashbordConfig = new DashboardOptions
            {
                AppPath = "#",
                IgnoreAntiforgeryToken = true,
                DisplayStorageConnectionString = hangfireSettings.DisplayStorageConnectionString,
                IsReadOnlyFunc = Context => false
            };

            if (hangfireSettings.HttpAuthInfo.IsOpenLogin && hangfireSettings.HttpAuthInfo.Users.Any())
            {
                var httpAuthInfo = hangfireSettings.HttpAuthInfo;
                var users = hangfireSettings.HttpAuthInfo.Users.Select(m => new BasicAuthAuthorizationUser
                {
                    Login = m.Login,
                    Password = m.Password,
                    PasswordClear = m.PasswordClear
                });

                var basicAuthAuthorizationFilterOptions = new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = httpAuthInfo.RequireSsl,
                    SslRedirect = httpAuthInfo.SslRedirect,
                    LoginCaseSensitive = httpAuthInfo.LoginCaseSensitive,
                    Users = users
                };

                dashbordConfig.Authorization = new[]
                {
                    new BasicAuthAuthorizationFilter(basicAuthAuthorizationFilterOptions)
                };

            }

            app.UseHangfireDashboard(hangfireSettings.StartUpPath, dashbordConfig);

            if (!string.IsNullOrEmpty(hangfireSettings.ReadOnlyPath))
                //只读面板，只能读取不能操作 
                app.UseHangfireDashboard(hangfireSettings.ReadOnlyPath, new DashboardOptions
                {
                    IgnoreAntiforgeryToken = true,
                    AppPath = hangfireSettings.StartUpPath, //返回时跳转的地址
                    DisplayStorageConnectionString = false, //是否显示数据库连接信息
                    IsReadOnlyFunc = Context => true
                });

            return app;
        }
        
    }

    public class HangfireSettings
    {
        public string TablePrefix { get; set; }
        public string StartUpPath { get; set; }
        public string ReadOnlyPath { get; set; }
        public List<string> JobQueues { get; set; }
        public HttpAuthInfo HttpAuthInfo { get; set; } = new HttpAuthInfo();
        public int WorkerCount { get; set; } = 40;
        public bool DisplayStorageConnectionString { get; set; } = false;
    }

    public class HttpAuthInfo
    {
        /// <summary>
        /// Redirects all non-SSL requests to SSL URL
        /// </summary>
        public bool SslRedirect { get; set; } = false;

        /// <summary>
        /// Requires SSL connection to access Hangfire dahsboard. It's strongly recommended to use SSL when you're using basic authentication.
        /// </summary>
        public bool RequireSsl { get; set; } = false;

        /// <summary>
        /// Whether or not login checking is case sensitive.
        /// </summary>
        public bool LoginCaseSensitive { get; set; } = true;

        public bool IsOpenLogin { get; set; } = true;

        public List<UserInfo> Users { get; set; } = new List<UserInfo>();
    }

    public class UserInfo
    {
        public string Login { get; set; }
        public string PasswordClear { get; set; }

        public byte[] Password => Encoding.UTF8.GetBytes(PasswordClear);
    }
}
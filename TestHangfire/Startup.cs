using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Hangfire.MySql.Core;
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
using Hangfire.Tags;
using Hangfire.Tags.Mysql;
using Microsoft.AspNetCore.Localization;
using Newtonsoft.Json;

namespace TestHangfire
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            JsonConfig = configuration;
        }

        public IConfiguration JsonConfig { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(Configuration); //Configuration是下面的方法
        }

        private void Configuration(IGlobalConfiguration globalConfiguration)
        {
            var mysqlOption = new MySqlStorageOptions
            {
                TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                JobExpirationCheckInterval = TimeSpan.FromHours(1),
                CountersAggregateInterval = TimeSpan.FromMinutes(5),
                PrepareSchemaIfNecessary = true,
                DashboardJobListLimit = 50000,
                TransactionTimeout = TimeSpan.FromMinutes(1),
                TablePrefix = "hangfire"
            };
            globalConfiguration.UseStorage(
                    new MySqlStorage(
                        JsonConfig.GetSection("HangfireMysqlConnectionString").Get<string>()
                        , mysqlOption
                    ))
                .UseConsole(new ConsoleOptions()
                {
                    BackgroundColor = "#000079"
                })
                .UseHangfireHttpJob(new HangfireHttpJobOptions
                {
                    MailOption = new MailOption
                    {
                        Server = JsonConfig.GetSection("HangfireMail:Server").Get<string>(),
                        Port = JsonConfig.GetSection("HangfireMail:Port").Get<int>(),
                        UseSsl = JsonConfig.GetSection("HangfireMail:UseSsl").Get<bool>(),
                        User = JsonConfig.GetSection("HangfireMail:User").Get<string>(),
                        Password = JsonConfig.GetSection("HangfireMail:Password").Get<string>(),
                    },
                    DefaultRecurringQueueName = JsonConfig.GetSection("DefaultRecurringQueueName").Get<string>(),
                    DefaultBackGroundJobQueueName = "DEFAULT",
                    DefaultTimeZone = "Asia/Shanghai",
                    //EnableDingTalk = true,
                    //CurrentDomain = "http://localhost:5000"
                    //RecurringJobTimeZone = TimeZoneInfo.Local,
                    // CheckHttpResponseStatusCode = code => (int)code < 400   //===》(default)
                    //AddHttpJobFilter = (jobContent) =>
                    //{
                    //    //添加httpjob的拦截器 如果返回false就代表不添加 返回true则真正的添加

                    //    if (jobContent.Url.StartsWith("http://localhost") ||
                    //        jobContent.Url.StartsWith("http://127.0.0.1"))
                    //    {
                    //        return true;
                    //    }

                    //    return false;
                    //}
                })
                .UseTagsWithMysql(sqlOptions: mysqlOption);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logging)
        {
            #region NLOG

            NLog.LogManager.LoadConfiguration("NLog.Config");
            logging.AddNLog();

            #endregion


            #region 强制显示中文
            var options = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh")
            };

            app.UseRequestLocalization(options);

            //强制显示中文
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh");

            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var queues = JsonConfig.GetSection("HangfireQueues").Get<List<string>>().ToArray();
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerTimeout = TimeSpan.FromMinutes(4),
                SchedulePollingInterval = TimeSpan.FromSeconds(15), //秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
                ShutdownTimeout = TimeSpan.FromMinutes(30), //超时时间
                Queues = queues, //队列
                WorkerCount = Math.Max(Environment.ProcessorCount, 40) //工作线程数，当前允许的最大线程，默认20
            });

            var hangfireStartUpPath = JsonConfig.GetSection("HangfireStartUpPath").Get<string>();
            if (string.IsNullOrWhiteSpace(hangfireStartUpPath)) hangfireStartUpPath = "/job";

            var dashbordConfig = new DashboardOptions
            {
                AppPath = "#",
                DisplayStorageConnectionString = false,
                IsReadOnlyFunc = Context => false
            };
            var dashbordUserName = JsonConfig.GetSection("HangfireUserName").Get<string>();
            var dashbordPwd = JsonConfig.GetSection("HangfirePwd").Get<string>();
            if (!string.IsNullOrEmpty(dashbordPwd) && !string.IsNullOrEmpty(dashbordUserName))
            {
                dashbordConfig.Authorization = new[]
                {
                    new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                    {
                        RequireSsl = false,
                        SslRedirect = false,
                        LoginCaseSensitive = true,
                        Users = new[]
                        {
                            new BasicAuthAuthorizationUser
                            {
                                Login = dashbordUserName,
                                PasswordClear = dashbordPwd
                            }
                        }
                    })
                };
            }
            app.UseHangfireDashboard(hangfireStartUpPath, dashbordConfig);

            var hangfireReadOnlyPath = JsonConfig.GetSection("HangfireReadOnlyPath").Get<string>();
            if (!string.IsNullOrWhiteSpace(hangfireReadOnlyPath))
            {
                //只读面板，只能读取不能操作
                app.UseHangfireDashboard(hangfireReadOnlyPath, new DashboardOptions
                {
                    IgnoreAntiforgeryToken = true,
                    AppPath = hangfireStartUpPath, //返回时跳转的地址
                    DisplayStorageConnectionString = false, //是否显示数据库连接信息
                    IsReadOnlyFunc = Context => true
                });
            }
           

            app.Run(async (context) => { await context.Response.WriteAsync(JsonConvert.SerializeObject(new { Success = false, Info = "ok" })); });
        }
    }
}
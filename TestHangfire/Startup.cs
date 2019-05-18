using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Hangfire.MySql.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace TestHangfire
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(Configuration);//Configuration是下面的方法
        }
        private void Configuration(IGlobalConfiguration globalConfiguration)
        {
            globalConfiguration.UseStorage(
                    new MySqlStorage(
                        "Server=localhost;Port=28747;Database=hangfire;Uid=root;Pwd=123456;charset=utf8;SslMode=none;Allow User Variables=True",
                        new MySqlStorageOptions
                        {
                            TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                            QueuePollInterval = TimeSpan.FromSeconds(15),
                            JobExpirationCheckInterval = TimeSpan.FromHours(1),
                            CountersAggregateInterval = TimeSpan.FromMinutes(5),
                            PrepareSchemaIfNecessary = true,
                            DashboardJobListLimit = 50000,
                            TransactionTimeout = TimeSpan.FromMinutes(1),
                            TablePrefix = "hangfire"
                        }))
                .UseConsole(new ConsoleOptions()
                {
                    BackgroundColor = "#000079"
                })
                .UseHangfireHttpJob(new HangfireHttpJobOptions
                {
                    MailOption = new MailOption
                    {
                        Server = "smtp.qq.com",
                        Port = 465,
                        UseSsl = true,
                        User = "admin@kawayiyi.com",
                        Password = "aqeumnudiimnchic"
                    },
                    DefaultRecurringQueueName = "recurring"
                });
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logging)
        {
            #region NLOG
            NLog.LogManager.LoadConfiguration("NLog.Config");
            logging.AddNLog();
            #endregion

            //var supportedCultures = new[]
            //{
            //    new CultureInfo("zh-CN"),
            //    new CultureInfo("en-US")
            //};
            //app.UseRequestLocalization(new RequestLocalizationOptions
            //{
            //    DefaultRequestCulture = new RequestCulture("zh-CN"),
            //    // Formatting numbers, dates, etc.
            //    SupportedCultures = supportedCultures,
            //    // UI strings that we have localized.
            //    SupportedUICultures = supportedCultures
            //});

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var queues = new[] { "default", "apis", "recurring" };
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerTimeout = TimeSpan.FromMinutes(4),
                SchedulePollingInterval = TimeSpan.FromSeconds(15),//秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
                ShutdownTimeout = TimeSpan.FromMinutes(30),//超时时间
                Queues = queues,//队列
                WorkerCount = Math.Max(Environment.ProcessorCount, 40)//工作线程数，当前允许的最大线程，默认20
            });

            app.UseHangfireDashboard("/job", new DashboardOptions
            {
                AppPath = "#",
                DisplayStorageConnectionString = false,
                IsReadOnlyFunc = Context => false,
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users = new []
                    {
                        new BasicAuthAuthorizationUser
                        {
                            Login = "admin",
                            PasswordClear =  "test"
                        }
                    }

                }) }
            });

            //只读面板，只能读取不能操作
            app.UseHangfireDashboard("/job-read", new DashboardOptions
            {
                IgnoreAntiforgeryToken = true,
                AppPath = "/job",//返回时跳转的地址
                DisplayStorageConnectionString = false,//是否显示数据库连接信息
                IsReadOnlyFunc = Context => true
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("ok.");
            });
        }
    }
}

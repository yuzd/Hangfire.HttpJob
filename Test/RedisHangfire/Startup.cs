using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Hangfire.Redis;
using Hangfire.Tags.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisHangfire
{
    public class Startup
    {
        public IConfiguration JsonConfig { get; }

        public Startup(IConfiguration configuration)
        {
            JsonConfig = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(Configuration); //Configuration是下面的方法
        }

        private void Configuration(IGlobalConfiguration globalConfiguration)
        {
            var redis = ConnectionMultiplexer.Connect(JsonConfig.GetSection("HangfireRedisConnectionString")
                .Get<string>());
            var options = new RedisStorageOptions
            {
                Prefix = "hangfire:",
                SucceededListSize = 9999,
                DeletedListSize = 4999,
                Db = redis.GetDatabase().Database
            };

            globalConfiguration.UseRedisStorage(redis,options)
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
                }).UseTagsWithRedis(redis, redisOptions: options);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #region 强制显示中文
            var options = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("zh")
            };

            app.UseRequestLocalization(options);

            //强制显示中文
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh");

            #endregion

            var queues = JsonConfig.GetSection("HangfireQueues").Get<List<string>>().ToArray();

            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                ServerTimeout = TimeSpan.FromMinutes(4),
                SchedulePollingInterval = TimeSpan.FromSeconds(2), //秒级任务需要配置短点，一般任务可以配置默认时间，默认15秒
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

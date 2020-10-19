using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Mvc;
using Hangfire.HttpAgent.Owin.Config;
using Hangfire.HttpAgent.RedisConsole.Owin;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Hangfire.HttpAgent.Owin.Cons.Startup))]

namespace Hangfire.HttpAgent.Owin.Cons
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddOwinHangfireJobAgent();
        }

        public void Configuration(IAppBuilder app)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var builder = new ConfigurationBuilder();
            var localtion = Path.GetDirectoryName(typeof(JobAgentApplicationBuilderExtensions).Assembly.Location);
            var appsettingPath = Path.Combine(localtion, "appsettings.json");

            builder.AddJsonFile(appsettingPath);

            var configRoot = builder.Build();
            services.Configure<JobAgentOptions>(configRoot.GetSection("JobAgent"));
            services.Configure<RedisStorageOptions>(configRoot.GetSection("RedisStorage"));

            ConfigureServices(services);
            var resolver = new DefaultDependencyResolver(services.BuildServiceProvider());
            DependencyResolver.SetResolver(resolver);


            app.UseOwinHangfireJobAgent(services);

            Console.WriteLine("任务启动成功");
        }
    }



}

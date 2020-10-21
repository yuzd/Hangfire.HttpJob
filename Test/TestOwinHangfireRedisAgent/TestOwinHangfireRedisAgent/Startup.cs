using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.RedisConsole;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Owin;


namespace TestOwinHangfireRedisAgent
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            NLog.LogManager.LoadConfiguration("NLog.Config");

            var builder = new ConfigurationBuilder();
            var localtion = Path.GetDirectoryName(typeof(Startup).Assembly.Location);
            var appsettingPath = Path.Combine(localtion, "appsettings.json");
            builder.AddJsonFile(appsettingPath);
            var configRoot = builder.Build();


            var services = new ServiceCollection()
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(configRoot);
                });

          
            services.AddSingleton<IConfiguration>(configRoot);
            services.AddHangfireJobAgent();
            app.UseHangfireJobAgent(services);
            Console.WriteLine("owin job agent ....");
        }
    }
}

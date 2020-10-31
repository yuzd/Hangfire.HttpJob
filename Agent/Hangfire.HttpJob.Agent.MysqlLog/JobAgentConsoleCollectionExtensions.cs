using Hangfire.HttpJob.Agent.MysqlConsole.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Hangfire.HttpJob.Agent.Config;
#if !NETCORE
using Microsoft.Extensions.Configuration;
using Owin;

#endif
namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    public static class JobAgentConsoleCollectionExtensions
    {

        public static IServiceCollection AddHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToMysql();
            return services;
        }


#if NETCORE

        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<MysqlConsoleServiceConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToMysql(configureStorageOptions);
            return app;
        }
#else
  public static IAppBuilder UseHangfireJobAgent(this IAppBuilder app,IServiceCollection services,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<MysqlConsoleServiceConfigurer> configureStorageOptions = null)
         {
            app.UseHangfireHttpJobAgent(services,configureOptions);
            app.UseJobAgentConsoleToMysql(services,configureStorageOptions);
            return app;
        }
#endif

        public static IServiceCollection AddJobAgentConsoleToMysql(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<MySqlStorageOptions>, MysqlConsoleOptionsConfigurer>();
            serviceCollection.TryAddSingleton<IHangfireStorage, MySqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, MysqlConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IMysqlStorageFactory>();
            return serviceCollection;
        }


#if NETCORE
        public static IApplicationBuilder UseJobAgentConsoleToMysql(this IApplicationBuilder app, Action<MysqlConsoleServiceConfigurer> configureOptions = null)
#else
        public static IAppBuilder UseJobAgentConsoleToMysql(this IAppBuilder app, IServiceCollection services,Action<MysqlConsoleServiceConfigurer> configureOptions = null)
#endif
        {
#if NETCORE
            var sp = app.ApplicationServices;
#else
            var sp = services.BuildServiceProvider();//OWIN
            var configRoot = sp.GetRequiredService<IConfiguration>();
            services.Configure<MySqlStorageOptions>(configRoot.GetSection("JobAgent:HangfireConsole"));
            sp = services.BuildServiceProvider();//OWIN
#endif
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MysqlConsole");
            var options = sp.GetService<IOptions<MySqlStorageOptions>>();
            var configurer = new MysqlConsoleServiceConfigurer(options.Value);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MysqlConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpJob.Agent.MysqlConsole】 - Failed to configure Hangfire.HttpJob.Agent.MysqlConsole middleware");
            }

            JobStorageConfig.LocalJobStorageConfig = new JobStorageConfig
            {
                Type = "mysql",
                HangfireDb = options.Value?.HangfireDb,
                TablePrefix = options.Value?.TablePrefix,
                ExpireAtDays = options.Value?.ExpireAtDays
            };

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.MysqlConsole】 - Registered MysqlConsole middleware Success!");

            return app;
        }
    }



}

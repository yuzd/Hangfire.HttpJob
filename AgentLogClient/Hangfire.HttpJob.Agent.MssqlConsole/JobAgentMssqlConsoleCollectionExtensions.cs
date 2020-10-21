using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Hangfire.HttpJob.Agent.MssqlConsole.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#if !NETCORE
using Microsoft.Extensions.Configuration;
using Owin;

#endif
namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public static class JobAgentMssqlConsoleCollectionExtensions
    {
        public static IServiceCollection AddHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToSqlServer();
            return services;
        }

#if NETCORE
        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<MssqlConsoleOptionsConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToSqlServer(configureStorageOptions);
            return app;
        }
#else
  public static IAppBuilder UseHangfireJobAgent(this IAppBuilder app,IServiceCollection services,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<MssqlConsoleOptionsConfigurer> configureStorageOptions = null)
         {
            app.UseHangfireHttpJobAgent(services,configureOptions);
            app.UseJobAgentConsoleToSqlServer(services,configureStorageOptions);
            return app;
        }
#endif



        public static IServiceCollection AddJobAgentConsoleToSqlServer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<MssqlStorageOptions>, MssqlConsoleOptions>();
            serviceCollection.TryAddSingleton<IHangfireStorage, MssqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, MssqlConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IMssqlStorageFactory>();
            return serviceCollection;
        }

#if NETCORE
        public static IApplicationBuilder UseJobAgentConsoleToSqlServer(this IApplicationBuilder app, Action<MssqlConsoleOptionsConfigurer> configureOptions = null)
#else
        public static IAppBuilder UseJobAgentConsoleToSqlServer(this IAppBuilder app, IServiceCollection services,Action<MssqlConsoleOptionsConfigurer> configureOptions = null)
#endif

        {

#if NETCORE
            var sp = app.ApplicationServices;
#else
            var sp = services.BuildServiceProvider();//OWIN
            var configRoot = sp.GetRequiredService<IConfiguration>();
            services.Configure<MssqlStorageOptions>(configRoot.GetSection("JobAgent:HangfireConsole"));
            sp = services.BuildServiceProvider();//OWIN
#endif
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MssqlConsole");
            var options = sp.GetService<IOptions<MssqlStorageOptions>>();
            var configurer = new MssqlConsoleOptionsConfigurer(options.Value);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MssqlConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpJob.Agent.MssqlConsole】 - Failed to configure Hangfire.HttpJob.Agent.MssqlConsole middleware");
            }

            JobStorageConfig.LocalJobStorageConfig = new JobStorageConfig
            {
                Type = "sqlserver",
                HangfireDb = options.Value?.HangfireDb,
                ExpireAtDays = options.Value?.ExpireAtDays
            };

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.MssqlConsole】 - Registered MssqlConsole middleware Success!");

            return app;
        }
    }
}

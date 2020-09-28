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


        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<MssqlConsoleOptionsConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToSqlServer(configureStorageOptions);
            return app;
        }

        public static IServiceCollection AddJobAgentConsoleToSqlServer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<MssqlStorageOptions>, MssqlConsoleOptions>();
            serviceCollection.TryAddSingleton<IHangfireStorage, MssqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, MssqlConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IMssqlStorageFactory>();
            return serviceCollection;
        }

        public static IApplicationBuilder UseJobAgentConsoleToSqlServer(this IApplicationBuilder app, Action<MssqlConsoleOptionsConfigurer> configureOptions = null)
        {
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MssqlConsole");
            var options = app.ApplicationServices.GetService<IOptions<MssqlStorageOptions>>();
            var configurer = new MssqlConsoleOptionsConfigurer(options.Value);
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
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

using System;
using Hangfire.HttpJob.Agent.Config;
using Hangfire.HttpJob.Agent.PostgreSqlConsole.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole
{
    public static class JobAgentConsoleCollectionExtensions
    {
        public static IServiceCollection AddHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToPostgreSql();
            return services;
        }

        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<PostgreSqlConsoleServiceConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToPostgreSql(configureStorageOptions);
            return app;
        }

        public static IServiceCollection AddJobAgentConsoleToPostgreSql(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<PostgreSqlStorageOptions>, PostgreSqlConsoleOptionsConfigurer>();
            serviceCollection.TryAddSingleton<IHangfireStorage, PostgreSqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, PostgreSqlConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IPostgreSqlStorageFactory>();
            return serviceCollection;
        }

        public static IApplicationBuilder UseJobAgentConsoleToPostgreSql(this IApplicationBuilder app,
            Action<PostgreSqlConsoleServiceConfigurer> configureOptions = null)
        {
            var appServices = app.ApplicationServices;
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.PostgreSqlConsole");
            var options = appServices.GetService<IOptions<PostgreSqlStorageOptions>>();
            var configurer = new PostgreSqlConsoleServiceConfigurer(options.Value);
            var loggerFactory = appServices.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<PostgreSqlConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpJob.Agent.PostgreSqlConsole】 - Failed to configure Hangfire.HttpJob.Agent.PostgreSqlConsole middleware");
            }

            JobStorageConfig.LocalJobStorageConfig = new JobStorageConfig
            {
                Type = "PostgreSQL",
                HangfireDb = options.Value?.HangfireDbConnString,
                TablePrefix = options.Value?.TablePrefix,
                ExpireAtDays = options.Value?.ExpireAtDays
            };

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.PostgreSqlConsole】 - Registered PostgreSqlConsole middleware Success!");
            return app;
        }
    }
}
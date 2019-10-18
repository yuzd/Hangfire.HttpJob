using System;
using System.Collections.Generic;
using System.Text;
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
        public static IServiceCollection AddJobAgentConsoleToSqlServer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<MssqlStorageOptions>, MssqlConsoleOptions>();
            serviceCollection.TryAddSingleton<IConsoleStorage, MssqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, MssqlConsole>();
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

            if (options.Value == null)
            {
                logger.LogCritical(evt, "【Hangfire.HttpJob.Agent.MssqlConsole】 - MssqlStorageOptions can not be null");
                return app;
            }

            if (string.IsNullOrEmpty(options.Value.HangfireDb))
            {
                throw new ArgumentException(nameof(MssqlStorageOptions.HangfireDb));
            }

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.MssqlConsole】 - Registered MssqlConsole middleware Success!");

            return app;
        }
    }
}

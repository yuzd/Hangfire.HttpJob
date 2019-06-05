using Hangfire.HttpJob.Agent.MysqlConsole.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    public static class JobAgentConsoleCollectionExtensions
    {
        public static IServiceCollection AddJobAgentConsoleToMysql(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<MySqlStorageOptions>, MysqlConsoleOptionsConfigurer>();
            serviceCollection.TryAddSingleton<IConsoleStorage, MySqlStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, MysqlConsole>();
            return serviceCollection;
        }



        public static IApplicationBuilder UseJobAgentConsoleToMysql(this IApplicationBuilder app, Action<MysqlConsoleServiceConfigurer> configureOptions = null)
        {
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MysqlConsole");
            var options = app.ApplicationServices.GetService<IOptions<MySqlStorageOptions>>();
            var configurer = new MysqlConsoleServiceConfigurer(options.Value);
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<MysqlConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpJob.Agent.MysqlConsole】 - Failed to configure Hangfire.HttpJob.Agent.MysqlConsole middleware");
            }

            if (options.Value == null)
            {
                logger.LogCritical(evt, "【Hangfire.HttpJob.Agent.MysqlConsole】 - MySqlStorageOptions can not be null");
                return app;
            }

            if (string.IsNullOrEmpty(options.Value.HangfireDb))
            {
                throw new ArgumentException(nameof(MySqlStorageOptions.HangfireDb));
            }

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.MysqlConsole】 - Registered MysqlConsole middleware Success!");

            return app;
        }
    }



}

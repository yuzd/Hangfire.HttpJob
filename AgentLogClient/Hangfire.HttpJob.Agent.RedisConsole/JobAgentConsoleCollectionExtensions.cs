using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Hangfire.HttpJob.Agent.Config;
using Hangfire.HttpJob.Agent.RedisConsole.Config;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    public static class JobAgentConsoleCollectionExtensions
    {

        public static IServiceCollection AddHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToRedis();
            return services;
        }



        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<RedisConsoleOptionsConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToRedis(configureStorageOptions);
            return app;
        }

        public static IServiceCollection AddJobAgentConsoleToRedis(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<RedisStorageOptions>, RedisConsoleOptions>();
            serviceCollection.TryAddSingleton<IHangfireStorage, RedisStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, RedisConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IRedisStorageFactory>();
            return serviceCollection;
        }



        public static IApplicationBuilder UseJobAgentConsoleToRedis(this IApplicationBuilder app, Action<RedisConsoleOptionsConfigurer> configureOptions = null)
        {
            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MysqlConsole");
            var options = app.ApplicationServices.GetService<IOptions<RedisStorageOptions>>();
            var configurer = new RedisConsoleOptionsConfigurer(options.Value);
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RedisConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpJob.Agent.RedisConsole】 - Failed to configure Hangfire.HttpJob.Agent.RedisConsole middleware");
            }

            JobStorageConfig.LocalJobStorageConfig = new JobStorageConfig
            {
                Type = "redis",
                HangfireDb = options.Value?.HangfireDb,
                Db = options.Value?.DataBase,
                ExpireAtDays = options.Value?.ExpireAtDays,
                TablePrefix = options.Value?.TablePrefix
            };

            logger.LogInformation(evt, "【Hangfire.HttpJob.Agent.RedisConsole】 - Registered RedisConsole middleware Success!");

            return app;
        }
    }



}

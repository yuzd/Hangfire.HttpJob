using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using Hangfire.HttpAgent.Owin.Config;
using Hangfire.HttpAgent.RedisConsole.Owin.Config;
using Hangfire.HttpAgent.RedisConsole.Owin;
using Hangfire.HttpAgent.Owin;
using Microsoft.Extensions.Logging;
using Owin;
using System.Linq;

namespace Hangfire.HttpAgent.RedisConsole.Owin
{
    public static class JobAgentConsoleCollectionExtensions
    {

        public static IServiceCollection AddOwinHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToRedis();
            return services;
        }


        public static IAppBuilder UseOwinHangfireJobAgent(this IAppBuilder app, IServiceCollection services,
          Action<JobAgentOptionsConfigurer> configureOptions = null, Action<RedisConsoleOptionsConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(services, configureOptions);
            app.UseJobAgentConsoleToRedis(services,configureStorageOptions);
            return app;
        }

        public static IServiceCollection AddHangfireJobAgent(this IServiceCollection services, Action<JobAgentServiceConfigurer> configure = null)
        {
            services.AddHangfireHttpJobAgent(configure);
            services.AddJobAgentConsoleToRedis();
            return services;
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



        public static IAppBuilder UseJobAgentConsoleToRedis(this IAppBuilder app, IServiceCollection services, Action<RedisConsoleOptionsConfigurer> configureOptions = null)
        {

            var sp = services.BuildServiceProvider();

            var evt = new EventId(1, "Hangfire.HttpJob.Agent.MysqlConsole");
            var options = sp.GetService<IOptions<RedisStorageOptions>>();
            var configurer = new RedisConsoleOptionsConfigurer(options.Value);
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RedisConsoleOptionsConfigurer>();
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "【Hangfire.HttpAgent.RedisConsole.Owin】 - Failed to configure Hangfire.HttpAgent.RedisConsole.Owin middleware");
            }

            JobStorageConfig.LocalJobStorageConfig = new JobStorageConfig
            {
                Type = "redis",
                HangfireDb = options.Value?.HangfireDb,
                Db = options.Value?.DataBase,
                ExpireAtDays = options.Value?.ExpireAtDays,
                TablePrefix = options.Value?.TablePrefix
            };

            logger.LogInformation(evt, "【Hangfire.HttpAgent.RedisConsole.Owin】 - Registered RedisConsole middleware Success!");

            return app;
        }
    }



}

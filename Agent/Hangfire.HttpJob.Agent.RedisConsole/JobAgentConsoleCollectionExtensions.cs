using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Hangfire.HttpJob.Agent.Config;
using Hangfire.HttpJob.Agent.RedisConsole.Config;
#if !NETCORE
using Microsoft.Extensions.Configuration;
using Owin;

#endif
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


#if NETCORE
        public static IApplicationBuilder UseHangfireJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<RedisConsoleOptionsConfigurer> configureStorageOptions = null)
        {
            app.UseHangfireHttpJobAgent(configureOptions);
            app.UseJobAgentConsoleToRedis(configureStorageOptions);
            return app;
        }
#else
  public static IAppBuilder UseHangfireJobAgent(this IAppBuilder app,IServiceCollection services,
            Action<JobAgentOptionsConfigurer> configureOptions = null, Action<RedisConsoleOptionsConfigurer> configureStorageOptions = null)
         {
            app.UseHangfireHttpJobAgent(services,configureOptions);
            app.UseJobAgentConsoleToRedis(services,configureStorageOptions);
            return app;
        }
#endif
        public static IServiceCollection AddJobAgentConsoleToRedis(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<RedisStorageOptions>, RedisConsoleOptions>();
            serviceCollection.TryAddSingleton<IHangfireStorage, RedisStorage>();
            serviceCollection.TryAddTransient<IHangfireConsole, RedisConsole>();
            serviceCollection.TryAddTransient<IStorageFactory, IRedisStorageFactory>();
            return serviceCollection;
        }


#if NETCORE
        public static IApplicationBuilder UseJobAgentConsoleToRedis(this IApplicationBuilder app, Action<RedisConsoleOptionsConfigurer> configureOptions = null)
#else
        public static IAppBuilder UseJobAgentConsoleToRedis(this IAppBuilder app, IServiceCollection services,Action<RedisConsoleOptionsConfigurer> configureOptions = null)
#endif
        {
#if NETCORE
            var sp = app.ApplicationServices;
#else
            var sp = services.BuildServiceProvider();//OWIN
            var configRoot = sp.GetRequiredService<IConfiguration>();
            services.Configure<RedisStorageOptions>(configRoot.GetSection("JobAgent:HangfireConsole"));
            sp = services.BuildServiceProvider();//OWIN
#endif
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

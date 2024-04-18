using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#if !NETCORE
using Owin;

#endif

namespace Hangfire.HttpJob.Agent
{
    public static class JobAgentApplicationBuilderExtensions
    {
#if NETCORE
        public static IApplicationBuilder UseHangfireHttpJobAgent(this IApplicationBuilder app,
           Action<JobAgentOptionsConfigurer> configureOptions = null)
#else
        public static IAppBuilder UseHangfireHttpJobAgent(this IAppBuilder app,IServiceCollection services,
            Action<JobAgentOptionsConfigurer> configureOptions = null)
#endif

        {

#if NETCORE
            var sp = app.ApplicationServices;
#else
            var sp = services.BuildServiceProvider();//OWIN
            var configRoot = sp.GetRequiredService<IConfiguration>();
            services.Configure<JobAgentOptions>(configRoot.GetSection("JobAgent"));
            sp = services.BuildServiceProvider();//OWIN
#endif
            var evt = new EventId(1, "Hangfire.HttpJob.Agent");
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<JobAgentMiddleware>();
            var options = sp.GetService<IOptions<JobAgentOptions>>();

            var configurer = new JobAgentOptionsConfigurer(options.Value);
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "Failed to configure JobAgent middleware");
            }

            if (!options.Value.Enabled)
            {
                return app;
            }


            if (string.IsNullOrEmpty(options.Value.SitemapUrl)) options.Value.SitemapUrl = "/jobagent";
            foreach (KeyValuePair<Type, JobMetaData> jobAgent in JobAgentServiceConfigurer.JobAgentDic)
            {
                logger.LogInformation(evt, $"【HttpJobAgent】 - [{jobAgent.Key.Name}] [Transient:{jobAgent.Value.Transien}] [HangJob:{jobAgent.Value.Hang}] - Registered");
            }
            var registerService = new JobAgentRegisterService(options, loggerFactory);

#if NETCORE
            var lifeRegister = sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IApplicationLifetime>();
            lifeRegister.ApplicationStarted.Register(async () =>
            {
                await registerService.StartAsync(CancellationToken.None);
            });
            lifeRegister.ApplicationStopping.Register(async () =>
            {
                await registerService.StopAsync(CancellationToken.None);
            });
#endif
            logger.LogInformation(evt, "【HttpJobAgent】 - Registered HttpJobAgent middleware to respond to {path}", new { path = options.Value.SitemapUrl });
#if NETCORE
            app.Map(options.Value.SitemapUrl, robotsApp =>
            {
                robotsApp.UseMiddleware<JobAgentMiddleware>();
            });
#else
            app.Map(options.Value.SitemapUrl, robotsApp =>
            {
                robotsApp.Use<JobAgentMiddleware>(logger, options, loggerFactory, sp);
            });

            registerService.StartAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
#endif

            return app;
        }
    }
}

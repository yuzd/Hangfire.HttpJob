using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent
{
    public static class JobAgentApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseHangfireHttpJobAgent(this IApplicationBuilder app,
            Action<JobAgentOptionsConfigurer> configureOptions = null)
        {
            var evt = new EventId(1, "Hangfire.HttpJob.Agent");
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<JobAgentMiddleware>();
            var options = app.ApplicationServices.GetService<IOptions<JobAgentOptions>>();
            var configurer = new JobAgentOptionsConfigurer(options.Value);
            try
            {
                configureOptions?.Invoke(configurer);
            }
            catch (Exception exception)
            {
                logger.LogCritical(evt, exception, "Failed to configure JobAgent middleware");
            }

            if (options.Value.Enabled)
            {
                if (string.IsNullOrEmpty(options.Value.SitemapUrl)) options.Value.SitemapUrl = "/jobagent";
                logger.LogInformation(evt, "【HttpJobAgent】 - Registered HttpJobAgent middleware to respond to {path}", new { path = options.Value.SitemapUrl });
                app.Map(options.Value.SitemapUrl, robotsApp =>
                {
                    robotsApp.UseMiddleware<JobAgentMiddleware>();
                });

                foreach (KeyValuePair<Type,JobMetaData > jobAgent in JobAgentServiceConfigurer.JobAgentDic)
                {
                    logger.LogInformation(evt, $"【HttpJobAgent】 - [{jobAgent.Key.Name}] [Transient:{jobAgent.Value.Transien}] [HangJob:{jobAgent.Value.Hang}] - Registered");
                }
            }
            return app;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Mvc;
using Hangfire.HttpAgent.Owin.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owin;

namespace Hangfire.HttpAgent.Owin
{
    public static class JobAgentApplicationBuilderExtensions
    {
        public static IAppBuilder UseHangfireHttpJobAgent(this IAppBuilder app, IServiceCollection services,
            Action<JobAgentOptionsConfigurer> configureOptions = null)
        {
            //services.AddLogging();
            //var builder = new ConfigurationBuilder();
            //var localtion = Path.GetDirectoryName(typeof(JobAgentApplicationBuilderExtensions).Assembly.Location);
            //var appsettingPath = Path.Combine(localtion, "appsettings.json");
            //builder.AddJsonFile(appsettingPath);
            //var configRoot = builder.Build();
            //services.Configure<JobAgentOptions>(configRoot.GetSection("jobagent"));
            //ConfigureServices(services);
            //var resolver = new DefaultDependencyResolver(services.BuildServiceProvider());
            //DependencyResolver.SetResolver(resolver);

            var sp = services.BuildServiceProvider();
            var evt = new EventId(1, "Hangfire.HttpAgent.Owin");
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<JobAgentOwinMiddleware>();
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

            if (options.Value.Enabled)
            {
                if (string.IsNullOrEmpty(options.Value.SitemapUrl)) options.Value.SitemapUrl = "/jobagent";
                logger.LogInformation(evt, "【HttpJobAgent】 - Registered HttpJobAgent middleware to respond to {path}", new { path = options.Value.SitemapUrl });
                app.Map(options.Value.SitemapUrl, robotsApp =>
                {
                    robotsApp.Use<JobAgentOwinMiddleware>(logger, options, loggerFactory, sp);
                });

                foreach (KeyValuePair<Type, JobMetaData> jobAgent in JobAgentServiceConfigurer.JobAgentDic)
                {
                    logger.LogInformation(evt, $"【HttpJobAgent】 - [{jobAgent.Key.Name}] [Transient:{jobAgent.Value.Transien}] [HangJob:{jobAgent.Value.Hang}] - Registered");
                }
            }
            return app;
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfireHttpJobAgent();
        }
    }


    public class DefaultDependencyResolver : IDependencyResolver
    {
        protected IServiceProvider serviceProvider;

        public DefaultDependencyResolver(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return this.serviceProvider.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.serviceProvider.GetServices(serviceType);
        }

        IEnumerable<object> IDependencyResolver.GetServices(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent
{
    public static class JobAgentServiceCollectionExtensions
    {
        public static IServiceCollection AddHangfireHttpJobAgent(this IServiceCollection serviceCollection, Action<JobAgentServiceConfigurer> configure = null)
        {
            serviceCollection.AddOptions();
            serviceCollection.TryAddSingleton<IConfigureOptions<JobAgentOptions>, ConfigureJobAgentOptions>();
            serviceCollection.AddSingleton<IHostedService, JobAgentRegisterService>();
            var configurer = new JobAgentServiceConfigurer(serviceCollection);
            if (configure == null)
            {
                var assembly = Assembly.GetEntryAssembly();
                configure = (c) => { c.AddJobAgent(assembly); };
            }
            configure.Invoke(configurer);
            serviceCollection.TryAddSingleton<JobAgentMiddleware>();
            return serviceCollection;

        }


    }
}

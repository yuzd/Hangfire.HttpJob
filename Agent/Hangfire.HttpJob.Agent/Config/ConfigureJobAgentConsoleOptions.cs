using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.Config
{
    public class ConfigureJobAgentConsoleOptions<T> : IConfigureOptions<T> where  T:class
    {
        private readonly IConfiguration configuration;

        public ConfigureJobAgentConsoleOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void Configure(T options)
        {
            configuration.GetSection("JobAgent:HangfireConsol").Bind(options);
        }

    }
}

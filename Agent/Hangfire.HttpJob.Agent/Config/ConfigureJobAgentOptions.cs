using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.Config
{
    public class ConfigureJobAgentOptions : IConfigureOptions<JobAgentOptions>
    {
        private readonly IConfiguration configuration;

        public ConfigureJobAgentOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void Configure(JobAgentOptions options)
        {
            configuration.GetSection("JobAgent").Bind(options);
        }

    }
}

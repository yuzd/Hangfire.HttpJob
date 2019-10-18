using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MssqlConsole.Config
{
    public class MssqlConsoleOptions : IConfigureOptions<MssqlStorageOptions>
    {
        private readonly IConfiguration configuration;

        public MssqlConsoleOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void Configure(MssqlStorageOptions options)
        {
            configuration.GetSection("JobAgent:HangfireConsole").Bind(options);
        }
    }
}

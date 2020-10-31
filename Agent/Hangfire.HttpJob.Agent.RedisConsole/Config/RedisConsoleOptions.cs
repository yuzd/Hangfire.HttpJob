using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.RedisConsole.Config
{
    public class RedisConsoleOptions : IConfigureOptions<RedisStorageOptions>
    {
        private readonly IConfiguration configuration;

        public RedisConsoleOptions(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void Configure(RedisStorageOptions options)
        {
            configuration.GetSection("JobAgent:HangfireConsole").Bind(options);
        }
    }
}

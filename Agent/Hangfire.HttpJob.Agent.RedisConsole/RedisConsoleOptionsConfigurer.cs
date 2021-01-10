using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.RedisConsole.Config
{
    public sealed class RedisConsoleOptionsConfigurer : ConfigureJobAgentConsoleOptions<RedisStorageOptions>
    {
        public RedisConsoleOptionsConfigurer(IConfiguration configuration) : base(configuration)
        {
        }
    }
}

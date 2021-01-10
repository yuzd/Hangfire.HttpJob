using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MssqlConsole.Config
{
    public sealed class MssqlConsoleOptionsConfigurer : ConfigureJobAgentConsoleOptions<MssqlStorageOptions>
    {
        public MssqlConsoleOptionsConfigurer(IConfiguration configuration) : base(configuration)
        {
        }
    }
}

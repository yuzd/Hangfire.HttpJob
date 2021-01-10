using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MysqlConsole.Config
{

    public sealed class MysqlConsoleOptionsConfigurer : ConfigureJobAgentConsoleOptions<MySqlStorageOptions>
    {
        public MysqlConsoleOptionsConfigurer(IConfiguration configuration) : base(configuration)
        {
        }
    }
}

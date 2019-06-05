using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MysqlConsole.Config
{

    internal class MysqlConsoleOptionsConfigurer : IConfigureOptions<MySqlStorageOptions>
    {
        private readonly IConfiguration configuration;

        public MysqlConsoleOptionsConfigurer(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public void Configure(MySqlStorageOptions options)
        {
            configuration.GetSection("JobAgent:HangfireConsole").Bind(options);
        }

    }
}

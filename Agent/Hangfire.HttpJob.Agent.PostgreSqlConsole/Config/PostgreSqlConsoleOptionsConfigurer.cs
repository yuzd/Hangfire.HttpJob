using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole.Config
{
    public class PostgreSqlConsoleOptionsConfigurer : IConfigureOptions<PostgreSqlStorageOptions>
    {
        private readonly IConfiguration configuration;
        public PostgreSqlConsoleOptionsConfigurer(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Configure(PostgreSqlStorageOptions options)
        {
            configuration.GetSection("JobAgent:HangfireConsole").Bind(options);
        }
    }
}
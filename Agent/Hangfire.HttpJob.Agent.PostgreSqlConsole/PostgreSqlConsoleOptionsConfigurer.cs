using Hangfire.HttpJob.Agent.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole.Config
{
    public sealed class PostgreSqlConsoleOptionsConfigurer : ConfigureJobAgentConsoleOptions<PostgreSqlStorageOptions>
    {
        public PostgreSqlConsoleOptionsConfigurer(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
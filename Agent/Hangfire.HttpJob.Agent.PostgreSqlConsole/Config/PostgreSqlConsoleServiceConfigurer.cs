namespace Hangfire.HttpJob.Agent.PostgreSqlConsole.Config
{
    public sealed class PostgreSqlConsoleServiceConfigurer
    {
        private readonly PostgreSqlStorageOptions options;

        internal PostgreSqlConsoleServiceConfigurer(PostgreSqlStorageOptions options)
        {
            this.options = options;
        }

        public PostgreSqlConsoleServiceConfigurer TablePrefix(string prefix)
        {
            options.TablePrefix = prefix;
            return this;
        }
        public PostgreSqlConsoleServiceConfigurer HangfireDbConnString(string dbConnString)
        {
            options.HangfireDbConnString = dbConnString;
            return this;
        }

        public PostgreSqlConsoleServiceConfigurer ExpireAtDays(int days)
        {
            options.ExpireAtDays = days;
            return this;
        }
    }
}
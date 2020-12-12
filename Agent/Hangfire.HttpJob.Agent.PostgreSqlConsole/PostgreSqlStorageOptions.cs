using System;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole
{
    public class PostgreSqlStorageOptions
    {
        // hangfire.PostgreSql不支持table prefix，此处留空
        public string TablePrefix { get; set; } = "";
        public string HangfireDbConnString { get; set; }
        public int ExpireAtDays { get; set; } = 7;
        public TimeSpan? ExpireAt { get; set; }
    }
}
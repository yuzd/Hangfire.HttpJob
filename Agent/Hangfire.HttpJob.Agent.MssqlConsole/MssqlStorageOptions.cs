using System;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlStorageOptions
    {
        public string HangfireDb { get; set; } 
        public string TablePrefix { get; set; } = "HangFire";

        public int ExpireAtDays { get; set; } = 7;

        public TimeSpan? ExpireAt { get; set; }
    }
}

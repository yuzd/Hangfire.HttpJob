using System;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlStorageOptions
    {
        public string HangfireDb { get; set; } = "HangFire";
        public string TablePrefix { get; set; }

        public int ExpireAtDays { get; set; } = 7;
    }
}

using System;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlStorageOptions
    {
        public string HangfireDb { get; set; }
        public string TablePrefix { get; set; }
    }
}

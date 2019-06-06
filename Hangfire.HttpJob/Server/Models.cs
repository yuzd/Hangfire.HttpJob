using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Server
{
    public class PauseRecurringJob
    {
        public string Id { get; set; }
    }


    public class JobDetailInfo
    {
        public string JobName { get; set; }
        public string Info { get; set; }
    }


    public class ConsoleInfo
    {
        public string SetKey { get; set; }
        public string HashKey { get; set; }
        public DateTime StartTime { get; set; }
    }
}

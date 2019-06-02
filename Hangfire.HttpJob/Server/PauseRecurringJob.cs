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
}

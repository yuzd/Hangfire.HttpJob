using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent
{
    public class JobContext
    {
        public string Param { get; set; }
        public IHangfireConsole Console { get; set; }
    }
}

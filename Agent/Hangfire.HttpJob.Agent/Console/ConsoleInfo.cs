using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent
{
    public class ConsoleInfo
    {
        public string SetKey { get; set; }
        public int ProgressBarId { get; set; }
        public string HashKey { get; set; }
        public DateTime StartTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    public class RedisStorageOptions
    {
        public string TablePrefix { get; set; } = "hangfire";
        public string HangfireDb { get; set; } 
        public int DataBase { get; set; } 
        public int ExpireAtDays { get; set; } = 7;
        public TimeSpan? ExpireAt { get; set; }
    }
}

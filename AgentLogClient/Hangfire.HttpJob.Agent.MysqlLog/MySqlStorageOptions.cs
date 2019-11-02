using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    public class MySqlStorageOptions
    {
        public string TablePrefix { get; set; } = "Hangfire";
        public string HangfireDb { get; set; } 
        public int ExpireAtDays { get; set; } = 7;

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    public class MySqlStorageOptions
    {
        public string TablePrefix { get; set; } = "Hangfire";
        public string DbConnectionString { get; set; } 

    }
}

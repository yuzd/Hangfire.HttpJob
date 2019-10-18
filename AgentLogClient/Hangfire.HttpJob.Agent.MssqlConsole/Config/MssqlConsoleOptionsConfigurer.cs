using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.MssqlConsole.Config
{
    public sealed class MssqlConsoleOptionsConfigurer
    {
        private readonly MssqlStorageOptions options;

        internal MssqlConsoleOptionsConfigurer(MssqlStorageOptions options)
        {
            this.options = options;
        }

        public MssqlConsoleOptionsConfigurer HangfireDb(string db)
        {
            options.HangfireDb = db;
            return this;
        }
    }
}

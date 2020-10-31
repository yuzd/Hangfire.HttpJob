using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.RedisConsole.Config
{
    public sealed class RedisConsoleOptionsConfigurer
    {
        private readonly RedisStorageOptions options;

        internal RedisConsoleOptionsConfigurer(RedisStorageOptions options)
        {
            this.options = options;
        }

        public RedisConsoleOptionsConfigurer HangfireDb(string db)
        {
            options.HangfireDb = db;
            return this;
        }
    }
}

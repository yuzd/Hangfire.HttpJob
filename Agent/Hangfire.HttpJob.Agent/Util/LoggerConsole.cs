using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hangfire.HttpJob.Agent.Util
{
    internal class LoggerConsole : HangfireConsole
    {
        private readonly ILogger _logger;
        public LoggerConsole(ILogger logger)
        {
            _logger = logger;
        }

        public override IHangfireStorage Storage => new LocalLoggerConsole(_logger);
    }

    internal class LocalLoggerConsole : IHangfireStorage
    {
        private readonly ILogger _logger;

        public LocalLoggerConsole(ILogger logger)
        {
            _logger = logger;
        }
        public void Dispose()
        {
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            foreach (var keyvalue in keyValuePairs)
            {
                _logger.LogInformation(new EventId(0, key), keyvalue.Key + "->" + keyvalue.Value);
            }

        }

        public void AddToSet(string key, string value, double score)
        {
            _logger.LogInformation(key + "->" + value);
        }
    }
}

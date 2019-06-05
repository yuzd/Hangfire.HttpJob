using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hangfire.HttpJob.Agent.Util
{
    internal class LoggerConsole: IHangfireConsole
    {
        private readonly ILogger _logger;
        public LoggerConsole(ILogger logger)
        {
            _logger = logger;
        }
        
        public void WriteLine(string message, ConsoleFontColor fontColor = null)
        {
            _logger.LogInformation(message);
        }
    }
}

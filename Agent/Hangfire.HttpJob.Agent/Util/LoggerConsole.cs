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

        public IProgressBar WriteProgressBar(string name, double value, ConsoleFontColor color = null)
        {
            return new NoOpProgressBar(this,name,value);
        }
    }

    internal class NoOpProgressBar : IProgressBar
    {
        private readonly LoggerConsole _console;
        private readonly string _name;
        private readonly double _value;

        public NoOpProgressBar(LoggerConsole console,string name, double value)
        {
            _console = console;
            _name = name;
            _value = value;
        }
        public void SetValue(int value)
        {
            SetValue((double)value);
        }

        public void SetValue(double value)
        {
            value = Math.Round(value, 1);

            if (value < 0 || value > 100)
                throw new ArgumentOutOfRangeException(nameof(value), "Value should be in range 0..100");

            _console.WriteLine($"[ProgressBar] {_name} --> {value} Of {_value}");
        }
    }
}

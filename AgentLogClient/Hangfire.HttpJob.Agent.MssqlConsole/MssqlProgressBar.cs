using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlProgressBar : IProgressBar
    {
        private readonly MssqlConsole _context;
        private readonly string _progressBarId;
        private string _name;
        private string _color;
        private double _value;

        internal MssqlProgressBar(MssqlConsole context, string progressBarId, string name,  ConsoleFontColor color = null)
        {
            if (string.IsNullOrEmpty(progressBarId))
                throw new ArgumentNullException(nameof(progressBarId));

            _context = context ?? throw new ArgumentNullException(nameof(context));
            _progressBarId = progressBarId;
            _name = name;
            _color = color;
            _value = -1;
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

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Interlocked.Exchange(ref _value, value) == value) return;

            _context.WriteBar(new ConsoleLine() { Message = _progressBarId, ProgressName = _name, ProgressValue = value, TextColor = _color });

            _name = null; // write name only once
            _color = null; // write color only once
        }
    }
}

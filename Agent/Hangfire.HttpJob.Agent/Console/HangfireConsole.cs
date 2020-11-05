using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent
{

    public abstract class HangfireConsole : IHangfireConsole, IHangfireConsoleInit
    {
        private double _lastTimeOffset = 0;
        private const int ValueFieldLimit = 256;
        private int _nextProgressBarId;

        protected ConsoleInfo ConsoleInfo;

        /// <summary>
        /// 每个storage类型不一样
        /// </summary>
        public abstract IHangfireStorage Storage { get; }



        internal void WriteBar(ConsoleLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            lock (this)
            {
                string value;

                line.TimeOffset = Math.Round((DateTime.UtcNow - ConsoleInfo.StartTime).TotalSeconds, 3);

                if (_lastTimeOffset >= line.TimeOffset)
                {
                    // prevent duplicate lines collapsing
                    line.TimeOffset = _lastTimeOffset + 0.0001;
                }

                _lastTimeOffset = line.TimeOffset;

                if (line.Message.Length > ValueFieldLimit - 36)
                {
                    // pretty sure it won't fit
                    // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
                    value = null;
                }
                else
                {
                    // try to encode and see if it fits
                    value = JsonConvert.SerializeObject(line);

                    if (value.Length > ValueFieldLimit)
                    {
                        value = null;
                    }
                }

                if (value == null)
                {
                    var referenceKey = Guid.NewGuid().ToString("N");

                    Storage.SetRangeInHash(ConsoleInfo.HashKey, new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JsonConvert.SerializeObject(line);
                }

                Storage.AddToSet(ConsoleInfo.SetKey, value, line.TimeOffset);
            }
        }

        public void WriteLine(string message, ConsoleFontColor fontColor = null)
        {
            if (this.ConsoleInfo == null || string.IsNullOrEmpty(message)) return;
            if (string.IsNullOrEmpty(this.ConsoleInfo.HashKey) || string.IsNullOrEmpty(this.ConsoleInfo.SetKey)) return;
            message = "【JobAgent】" + message;
            lock (this)
            {
                string value;

                ConsoleLine line = new ConsoleLine
                {
                    Message = message
                };

                if (fontColor != null)
                {
                    line.TextColor = fontColor.ToString();
                }

                line.TimeOffset = Math.Round((DateTime.UtcNow - ConsoleInfo.StartTime).TotalSeconds, 3);
                if (_lastTimeOffset >= line.TimeOffset)
                {
                    // prevent duplicate lines collapsing
                    line.TimeOffset = _lastTimeOffset + 0.0001;
                }

                _lastTimeOffset = line.TimeOffset;

                if (line.Message.Length > ValueFieldLimit - 36)
                {
                    // pretty sure it won't fit
                    // (36 is an upper bound for JSON formatting, TimeOffset and TextColor)
                    value = null;
                }
                else
                {
                    // try to encode and see if it fits
                    value = JsonConvert.SerializeObject(line);

                    if (value.Length > ValueFieldLimit)
                    {
                        value = null;
                    }
                }

                if (value == null)
                {
                    var referenceKey = Guid.NewGuid().ToString("N");

                    Storage.SetRangeInHash(ConsoleInfo.HashKey, new[] { new KeyValuePair<string, string>(referenceKey, line.Message) });

                    line.Message = referenceKey;
                    line.IsReference = true;

                    value = JsonConvert.SerializeObject(line);
                }

                Storage.AddToSet(ConsoleInfo.SetKey, value, line.TimeOffset);
            }
        }

        public IProgressBar WriteProgressBar(string name, double initValue = 1, ConsoleFontColor color = null)
        {
            var progressBarId = Interlocked.Increment(ref _nextProgressBarId);

            var progressBar = new HangfireProgressBar(this, progressBarId.ToString(CultureInfo.InvariantCulture), name, color);
            // set initial value
            progressBar.SetValue(initValue);

            return progressBar;
        }
        public void Error(string err)
        {
            WriteLine(err, ConsoleFontColor.Red);
        }
        public void Error(Exception err)
        {
            WriteLine(err.ToString(), ConsoleFontColor.Red);
        }

        public void Info(string info)
        {
            WriteLine(info, ConsoleFontColor.White);
        }

        public void Warning(string warn)
        {
            WriteLine(warn, ConsoleFontColor.Yellow);
        }
        public void Warning(Exception warn)
        {
            WriteLine(warn.ToString(), ConsoleFontColor.Yellow);
        }

        public void Init(ConsoleInfo consoleInfo)
        {
            if (consoleInfo == null) throw new ArgumentNullException(nameof(ConsoleInfo));
            ConsoleInfo = consoleInfo;
            _nextProgressBarId = consoleInfo.ProgressBarId;//初始化

            if (consoleInfo != null && consoleInfo.StartTime == DateTime.MinValue)
            {
                consoleInfo.StartTime = DateTime.UtcNow;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent
{

    public interface IConsoleStorage
    {
        void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);

        void AddToSet(string key, string value, double score);
    }

    public interface IHangfireConsole
    {
        void WriteLine(string message, ConsoleFontColor fontColor = null);
        IProgressBar WriteProgressBar(string name, double initValue=1, ConsoleFontColor color = null);
    }

    /// <summary>
    /// Progress bar line inside console.
    /// </summary>
    public interface IProgressBar
    {
        /// <summary>
        /// Updates a value of a progress bar.
        /// </summary>
        /// <param name="value">New value</param>
        void SetValue(int value);

        /// <summary>
        /// Updates a value of a progress bar.
        /// </summary>
        /// <param name="value">New value</param>
        void SetValue(double value);
    }

    public interface IHangfireConsoleInit
    {
        void Init(ConsoleInfo consoleInfo);
    }

    public class ConsoleInfo
    {
        public string SetKey { get; set; }
        public int ProgressBarId { get; set; }
        public string HashKey { get; set; }
        public DateTime StartTime { get; set; }
    }


    public class ConsoleFontColor
    {
        /// <summary>The color black.</summary>
        public static readonly ConsoleFontColor Black = new ConsoleFontColor("#000000");
        /// <summary>The color dark blue.</summary>
        public static readonly ConsoleFontColor DarkBlue = new ConsoleFontColor("#000080");
        /// <summary>The color dark green.</summary>
        public static readonly ConsoleFontColor DarkGreen = new ConsoleFontColor("#008000");
        /// <summary>The color dark cyan (dark blue-green).</summary>
        public static readonly ConsoleFontColor DarkCyan = new ConsoleFontColor("#008080");
        /// <summary>The color dark red.</summary>
        public static readonly ConsoleFontColor DarkRed = new ConsoleFontColor("#800000");
        /// <summary>The color dark magenta (dark purplish-red).</summary>
        public static readonly ConsoleFontColor DarkMagenta = new ConsoleFontColor("#800080");
        /// <summary>The color dark yellow (ochre).</summary>
        public static readonly ConsoleFontColor DarkYellow = new ConsoleFontColor("#808000");
        /// <summary>The color gray.</summary>
        public static readonly ConsoleFontColor Gray = new ConsoleFontColor("#c0c0c0");
        /// <summary>The color dark gray.</summary>
        public static readonly ConsoleFontColor DarkGray = new ConsoleFontColor("#808080");
        /// <summary>The color blue.</summary>
        public static readonly ConsoleFontColor Blue = new ConsoleFontColor("#0000ff");
        /// <summary>The color green.</summary>
        public static readonly ConsoleFontColor Green = new ConsoleFontColor("#00ff00");
        /// <summary>The color cyan (blue-green).</summary>
        public static readonly ConsoleFontColor Cyan = new ConsoleFontColor("#00ffff");
        /// <summary>The color red.</summary>
        public static readonly ConsoleFontColor Red = new ConsoleFontColor("#ff0000");
        /// <summary>The color magenta (purplish-red).</summary>
        public static readonly ConsoleFontColor Magenta = new ConsoleFontColor("#ff00ff");
        /// <summary>The color yellow.</summary>
        public static readonly ConsoleFontColor Yellow = new ConsoleFontColor("#ffff00");
        /// <summary>The color white.</summary>
        public static readonly ConsoleFontColor White = new ConsoleFontColor("#ffffff");
        private readonly string _color;

        private ConsoleFontColor(string color)
        {
            this._color = color;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this._color;
        }

        /// <summary>
        /// Implicitly converts <see cref="T:Hangfire.Console.ConsoleTextColor" /> to <see cref="T:System.String" />.
        /// </summary>
        public static implicit operator string(ConsoleFontColor color)
        {
            return color?._color;
        }
    }
}

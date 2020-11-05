using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent
{
    
    public interface IHangfireStorage:IDisposable
    {
        void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs);

        void AddToSet(string key, string value, double score);
    }

    public interface ICommonConsole
    {
        void Error(string err);
        void Error(Exception err);
        void Info(string info);
        void Warning(string warn);

        void Warning(Exception warn);
    }

    public interface IHangfireConsole: ICommonConsole
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

  

}

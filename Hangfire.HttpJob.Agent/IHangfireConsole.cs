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
        void WriteLine(string message);
    }
    public interface IHangfireConsoleInit
    {
        void Init(ConsoleInfo consoleInfo);
    }

    public class ConsoleInfo
    {
        public string SetKey { get; set; }
        public string HashKey { get; set; }
        public DateTime StartTime { get; set; }
    }
}

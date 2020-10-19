using System;

namespace Hangfire.Heartbeat.Server
{
    internal class ProcessInfo
    {
        public int Id { get; set; }
        public string Server { get; set; }
        public string ProcessName { get; set; }
        public double CpuUsage { get; set; }
        public long WorkingSet { get; set; }
        public long DiskUsage { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}

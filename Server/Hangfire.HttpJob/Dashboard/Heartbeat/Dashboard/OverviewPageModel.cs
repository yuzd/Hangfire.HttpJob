using Hangfire.Annotations;

namespace Hangfire.Heartbeat.Dashboard
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal class ServerView
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public bool Error { get; set; }
        public string ProcessId { get; set; }
        public string ProcessName { get; set; }
        public double CpuUsagePercentage { get; set; }
        public long WorkingMemorySet { get; set; }
        public long Timestamp { get; set; }
        public long DiskUsage { get; set; }
    }
}

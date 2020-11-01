using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Hangfire.HttpJob.Agent
{
    /// <summary>
    /// 策略是 调度端会每隔一段时间来发送，发送一次客户端就提供30分钟内的心跳 每1s一次 每种storage在心跳期间只会存在一个实例
    /// </summary>
    class HeartBeatReport
    {
        private volatile string _currenturl;
        private volatile string _serverId;
        private IHangfireStorage _hangfireStorage;
        private static readonly ConcurrentDictionary<string, HeartBeatReport> _cache = new ConcurrentDictionary<string, HeartBeatReport>();
        private static System.Threading.Timer mDetectionTimer;
        private volatile int times = 5 * 60;

        private readonly Process _process;
        private readonly int _processorCount;
        private readonly TimeSpan _checkInterval;
        private (TimeSpan? current, TimeSpan? next) _processorTimeUsage;

        public HeartBeatReport(string serverId, string currenturl, IHangfireStorage hangfireStorage)
        {
            _serverId = serverId;
            _currenturl = currenturl;
            _hangfireStorage = hangfireStorage;
            mDetectionTimer = new System.Threading.Timer(OnVerify, null, 1000 * 1, 1000 * 1);
            this._process = Process.GetCurrentProcess();
            _processorCount = Environment.ProcessorCount;
            _checkInterval = TimeSpan.FromSeconds(1);
        }

        public static void ReportHeartBeat(string serverId, string currenturl, IHangfireStorage hangfireStorage)
        {
            if (!_cache.TryGetValue(serverId, out var reporter))
            {
                reporter = new HeartBeatReport(serverId, currenturl, hangfireStorage);
                _cache.TryAdd(serverId, reporter);
                return;
            }

            reporter.ReStart(serverId, currenturl, hangfireStorage);
        }


        private void OnVerify(object state)
        {
            lock (this)
            {
                mDetectionTimer.Change(-1, -1);
                try
                {
                    if (_processorTimeUsage.current.HasValue && _processorTimeUsage.next.HasValue)
                    {
                        var cpuPercentUsage = ComputeCpuUsage(_processorTimeUsage.current.Value, _processorTimeUsage.next.Value);
                        var data = new ProcessInfo
                        {
                            Id = _process.Id,
                            Idx = this.times,
                            Server = _currenturl,
                            ProcessName = _process.ProcessName,
                            CpuUsage = cpuPercentUsage,
                            WorkingSet = _process.WorkingSet64,
                            Timestamp = DateTimeOffset.UtcNow
                        };
                        string fullPath = _process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            string rootDir = Directory.GetDirectoryRoot(fullPath);
                            DriveInfo driveInfo = new DriveInfo(rootDir);
                            data.DiskUsage = driveInfo.AvailableFreeSpace;
                        }
                        var values = new Dictionary<string, string>
                        {
                            [_currenturl] = Newtonsoft.Json.JsonConvert.SerializeObject(data)
                        };

                        this._hangfireStorage.SetRangeInHash("AgentHeart:" + _serverId, values);
                    }

                    _process.Refresh();
                    var next = _process.TotalProcessorTime;
                    _processorTimeUsage = (_processorTimeUsage.next, next);
                }
#pragma warning disable 168
                catch(Exception exception)
#pragma warning restore 168
                {
                    // ignored
                }
                finally
                {
                    Interlocked.Decrement(ref this.times);

                    if (this.times < 1)
                    {
                        this.times = 0;
                    }

                    if (this.times > 0)
                    {
                        mDetectionTimer.Change(1000 * 1, 1000 * 1);
                    }
                }
            }
        }

        private double ComputeCpuUsage(TimeSpan current, TimeSpan next)
        {
            var totalMilliseconds = (next - current).TotalMilliseconds;
            var totalCpuPercentUsage = (totalMilliseconds / _checkInterval.TotalMilliseconds) * 100;
            var cpuPercentUsage = totalCpuPercentUsage / _processorCount;
            return Math.Round(cpuPercentUsage, 1);
        }



        private void ReStart(string serverId, string currenturl, IHangfireStorage hangfireStorage)
        {
            lock (this)
            {
                _serverId = serverId;
                _currenturl = currenturl;
                _hangfireStorage = hangfireStorage;
                var isStoped = this.times < 1;
                this.times = 5 * 60;//重置
                if (isStoped) mDetectionTimer.Change(1000 * 1, 1000 * 1);
            }
        }
    }


    internal class ProcessInfo
    {
        public int Id { get; set; }
        public int Idx { get; set; }
        public string ProcessName { get; set; }
        public string Server { get; set; }
        public double CpuUsage { get; set; }
        public long WorkingSet { get; set; }
        public long DiskUsage { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}

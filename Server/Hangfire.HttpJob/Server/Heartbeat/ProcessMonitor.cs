using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire.Heartbeat.Server
{
    /// <summary>
    /// Process monitor for Hangfire.
    /// </summary>
    [PublicAPI]
    public sealed class ProcessMonitor : IBackgroundProcess
    {
        private readonly Process _process;
        private readonly TimeSpan _checkInterval;
        private readonly int _processorCount;
        private readonly TimeSpan _expireIn;
        private (TimeSpan? current, TimeSpan? next) _processorTimeUsage;

        internal static string CurrentServerId = "";
        public ProcessMonitor()
            : this(TimeSpan.FromSeconds(1), Process.GetCurrentProcess())
        {

        }
        
        /// <summary>
        /// Creates process monitor with current process.
        /// </summary>
        /// <param name="checkInterval">Period between checks.</param>
        public ProcessMonitor(TimeSpan checkInterval)
            : this(checkInterval, Process.GetCurrentProcess())
        {

        }

        /// <summary>
        /// Creates process monitor with specified process.
        /// </summary>
        /// <param name="checkInterval">Period between checks.</param>
        /// <param name="processToMonitor">Process to monitor.</param>
        public ProcessMonitor(TimeSpan checkInterval, Process processToMonitor)
        {
            if (checkInterval == TimeSpan.Zero)
                throw new ArgumentException("Check interval must be nonzero value.", nameof(checkInterval));

            if (checkInterval != checkInterval.Duration())
                throw new ArgumentException("Check interval must be positive value.", nameof(checkInterval));

            _checkInterval = checkInterval;
            _process = processToMonitor;
            _expireIn = _checkInterval + TimeSpan.FromMinutes(1);
            _processorCount = Environment.ProcessorCount;
            _processorTimeUsage = default;
        }

        /// <inheritdoc/>
        public void Execute(BackgroundProcessContext context)
        {
            try
            {
                CurrentServerId = context.ServerId;
                if (context.IsStopping)
                {
                    CleanupState(context);
                    return;
                }

                if (_processorTimeUsage.current.HasValue && _processorTimeUsage.next.HasValue)
                {
                    var cpuPercentUsage = ComputeCpuUsage(_processorTimeUsage.current.Value, _processorTimeUsage.next.Value);

                    WriteState(context, cpuPercentUsage);
                }

                context.Wait(_checkInterval);
                _process.Refresh();

                var next = _process.TotalProcessorTime;
                _processorTimeUsage = (_processorTimeUsage.next, next);
            }
            catch (Exception)
            {
                //ignore
            }
          
        }

        private void WriteState(BackgroundProcessContext context, double cpuPercentUsage)
        {
            using (var connection = context.Storage.GetConnection())
            using (var writeTransaction = connection.CreateWriteTransaction())
            {
                
                var key = Utils.FormatKey(context.ServerId);
                var data = new ProcessInfo
                {
                    Id = _process.Id,
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
                    [data.Id.ToString()] = SerializationHelper.Serialize(data)
                };

                writeTransaction.SetRangeInHash(key, values);

                // if storage supports manual expiration handling
                if (writeTransaction is JobStorageTransaction jsTransaction)
                {
                    jsTransaction.ExpireHash(key, _expireIn);
                }

                writeTransaction.Commit();
            }
        }

        private double ComputeCpuUsage(TimeSpan current, TimeSpan next)
        {
            var totalMilliseconds = (next - current).TotalMilliseconds;
            var totalCpuPercentUsage = (totalMilliseconds / _checkInterval.TotalMilliseconds) * 100;
            var cpuPercentUsage = totalCpuPercentUsage / _processorCount;
            return Math.Round(cpuPercentUsage, 1);
        }

        private static void CleanupState(BackgroundProcessContext context)
        {
            using (var connection = context.Storage.GetConnection())
            using (var transaction = connection.CreateWriteTransaction())
            {
                var key = Utils.FormatKey(context.ServerId);
                transaction.RemoveHash(key);
                transaction.Commit();
            }
        }
    }
}

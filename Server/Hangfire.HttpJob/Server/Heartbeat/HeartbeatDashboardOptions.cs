using System;

namespace Hangfire.Heartbeat
{
    /// <summary>
    /// Contains initialization options for Hangfire Dashboard UI.
    /// </summary>
    public sealed class HeartbeatDashboardOptions
    {
        /// <summary>
        /// Status polling interval.
        /// </summary>
        public TimeSpan CheckInterval { get; }

        /// <summary>
        /// Creates new options.
        /// </summary>
        /// <param name="checkInterval">Status polling interval.</param>
        public HeartbeatDashboardOptions(TimeSpan checkInterval)
        {
            if (checkInterval == TimeSpan.Zero) throw new ArgumentException("Check interval must be nonzero value.", nameof(checkInterval));
            if (checkInterval != checkInterval.Duration()) throw new ArgumentException("Check interval must be positive value.", nameof(checkInterval));

            CheckInterval = checkInterval;
        }
    }
}

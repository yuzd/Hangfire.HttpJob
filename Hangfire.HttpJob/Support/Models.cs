using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Support
{
    internal class ConsoleId : IEquatable<ConsoleId>
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private string _cachedString;

        /// <summary>
        /// Job identifier
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Timestamp
        /// </summary>
        public long Timestamp { get; }

        /// <summary>
        /// <see cref="Timestamp"/> value as <see cref="DateTime"/>.
        /// </summary>
        public DateTime DateValue => UnixEpoch.AddMilliseconds(Timestamp);

        /// <summary>
        /// Initializes an instance of <see cref="ConsoleId"/>
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="timestamp">Timestamp</param>
        public ConsoleId(string jobId, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentNullException(nameof(jobId));

            JobId = jobId;
            Timestamp = (long)(timestamp - UnixEpoch).TotalMilliseconds;

            if (Timestamp <= 0 || Timestamp > int.MaxValue * 1000L)
                throw new ArgumentOutOfRangeException(nameof(timestamp));
        }

        /// <summary>
        /// Initializes an instance of <see cref="ConsoleId"/>.
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="timestamp">Timestamp</param>
        private ConsoleId(string jobId, long timestamp)
        {
            JobId = jobId;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Creates an instance of <see cref="ConsoleId"/> from string representation.
        /// </summary>
        /// <param name="value">String</param>
        public static ConsoleId Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length < 12)
                throw new ArgumentException("Invalid value", nameof(value));

            // Timestamp is serialized in reverse order for better randomness!

            long timestamp = 0;
            for (var i = 10; i >= 0; i--)
            {
                var c = value[i] | 0x20;

                var x = (c >= '0' && c <= '9') ? (c - '0') : (c >= 'a' && c <= 'f') ? (c - 'a' + 10) : -1;
                if (x == -1)
                    throw new ArgumentException("Invalid value", nameof(value));

                timestamp = (timestamp << 4) + x;
            }

            return new ConsoleId(value.Substring(11), timestamp) { _cachedString = value };
        }

        /// <inheritdoc />
        public bool Equals(ConsoleId other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;

            return other.Timestamp == Timestamp
                && other.JobId == JobId;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (_cachedString == null)
            {
                var buffer = new char[11 + JobId.Length];

                var timestamp = Timestamp;
                for (var i = 0; i < 11; i++, timestamp >>= 4)
                {
                    var c = timestamp & 0x0F;
                    buffer[i] = (c < 10) ? (char)(c + '0') : (char)(c - 10 + 'a');
                }

                JobId.CopyTo(0, buffer, 11, JobId.Length);

                _cachedString = new string(buffer);
            }

            return _cachedString;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as ConsoleId);

        /// <inheritdoc />
        public override int GetHashCode() => (JobId.GetHashCode() * 17) ^ Timestamp.GetHashCode();
    }
}

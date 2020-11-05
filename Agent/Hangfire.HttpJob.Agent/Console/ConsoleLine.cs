using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent
{
    internal class ConsoleLine
    {
        /// <summary>
        /// Time offset since console timestamp in fractional seconds
        /// </summary>
        [JsonProperty("t", Required = Required.Always)]
        public double TimeOffset { get; set; }

        /// <summary>
        /// True if <see cref="Message"/> is a Hash reference.
        /// </summary>
        [JsonProperty("r", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsReference { get; set; }

        /// <summary>
        /// Message text, or message reference, or progress bar id
        /// </summary>
        [JsonProperty("s", Required = Required.Always)]
        public string Message { get; set; }

        /// <summary>
        /// Text color for this message
        /// </summary>
        [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TextColor { get; set; }

        /// <summary>
        /// Value update for a progress bar
        /// </summary>
        [JsonProperty("p", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public double? ProgressValue { get; set; }

        /// <summary>
        /// Optional name for a progress bar
        /// </summary>
        [JsonProperty("n", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProgressName { get; set; }
    }
}

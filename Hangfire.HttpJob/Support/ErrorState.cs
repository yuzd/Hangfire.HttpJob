using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.Common;
using Hangfire.States;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Support
{
    internal class ErrorState:IState
    {
        public static readonly string StateName = "Failed";
        private readonly string _title;

        public ErrorState(string err,string title = "HttpJob")
        {
            FailedAt = DateTime.UtcNow;
            Reason = err;
            _title = string.IsNullOrEmpty(title) ? "HttpJob" : title;
        }

        [JsonIgnore]
        public DateTime FailedAt { get; }

        [JsonIgnore]
        public string Name => StateName;



        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "FailedAt", JobHelper.SerializeDateTime(FailedAt) },
                { "ExceptionType", _title },
                { "ExceptionMessage", Reason },
                { "ExceptionDetails", ""}
            };
        }

        public string Reason { get; set; }


        [JsonIgnore]
        public bool IsFinal => false;
        [JsonIgnore]
        public bool IgnoreJobLoadException => false;
    }
}

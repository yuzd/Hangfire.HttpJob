using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire.HttpJob.Support
{

    internal class JobHistorySaveTimeFilterAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly TimeSpan _timeSpan;
        public JobHistorySaveTimeFilterAttribute(TimeSpanType  timeSpanType, int timeout)
        {
            switch (timeSpanType)
            {
                case TimeSpanType.Second:
                    _timeSpan = TimeSpan.FromSeconds(timeout);
                    break;
                case TimeSpanType.Minute:
                    _timeSpan = TimeSpan.FromMinutes(timeout);
                    break;
                case TimeSpanType.Hour:
                    _timeSpan = TimeSpan.FromHours(timeout);
                    break;
                case TimeSpanType.Day:
                    _timeSpan = TimeSpan.FromDays(timeout);
                    break;
            }
        }
        public void OnStateApplied(ApplyStateContext filterContext, IWriteOnlyTransaction transaction)
        {
            filterContext.JobExpirationTimeout = _timeSpan;
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            context.JobExpirationTimeout = _timeSpan;
        }


       
    }

    enum TimeSpanType
    {
        Second,
        Minute,
        Hour,
        Day
    }
}

using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;
using Hangfire.HttpJob.Server;

namespace Hangfire.HttpJob.Support
{
    /// <summary>
    ///     自定义queue名称
    /// </summary>
    public class QueueProviderFilter : IJobFilterProvider
    {
        public IEnumerable<Common.JobFilter> GetFilters(Job job)
        {
            var arg = job.Args.FirstOrDefault() as HttpJobItem;
            if (arg == null || string.IsNullOrEmpty(arg.QueueName)) return new Common.JobFilter[] { };


            return new[]
            {
                new Common.JobFilter(
                    new QueueAttribute(arg.QueueName.ToLower()),
                    JobFilterScope.Method, null
                )
            };
        }
    }
}
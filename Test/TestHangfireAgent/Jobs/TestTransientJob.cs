using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestHangfireAgent.Jobs
{
    [TransientJob]
    public class TestTransientJob:JobAgent
    {
        private readonly ILogger<TestTransientJob> _logger;

        public TestTransientJob(ILogger<TestTransientJob> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Create {nameof(TestTransientJob)} Instance Success");
        }
        public override async Task OnStart(JobContext jobContext)
        {
            _logger.LogWarning("ManagedThreadId:" + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(5000);
            _logger.LogWarning(nameof(OnStart) + (jobContext.Param ?? string.Empty));
        }

        public override void OnStop(JobContext jobContext)
        {
            _logger.LogInformation("OnStop");
        }

        public override void OnException(JobContext jobContext,Exception ex)
        {
            _logger.LogError(ex, nameof(OnException) + (ex.Data["Method"] ?? string.Empty));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestSqlserverHangfireAgent.Jobs
{
    [HangJobUntilStop(true)]
    public class TestHangJob : JobAgent
    {
        private readonly ILogger<TestHangJob> _logger;

        public TestHangJob(ILogger<TestHangJob> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Create {nameof(TestHangJob)} Instance Success");
        }
        public override async Task OnStart(JobContext jobContext)
        {
            await Task.Delay(1000 * 10);
           
            _logger.LogWarning(nameof(OnStart) + (jobContext.Param ?? string.Empty));

        }

        
    }
}

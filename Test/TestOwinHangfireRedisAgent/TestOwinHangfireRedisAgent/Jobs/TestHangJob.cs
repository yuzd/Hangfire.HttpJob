using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestOwinHangfireRedisAgent.Jobs
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
            jobContext.Console.Info(nameof(OnStart) + (jobContext.Param ?? string.Empty));

            while (!jobContext.CancelToken.IsCancellationRequested)
            {
                jobContext.Console.Info("dddd");
                await Task.Delay(1000 * 10);
            }
            throw new Exception("dddddd");
            jobContext.Console.Warning("game over");
        }

    }
}

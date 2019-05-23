using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestHangfireAgent.Jobs
{
    public class TestJob:JobAgent
    {
        private readonly ILogger<TestJob> _logger;

        public TestJob(ILogger<TestJob> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Create {nameof(TestJob)} Instance Success");
        }
        protected override async Task OnStart(string param)
        {
            await Task.Delay(1000 * 10);
            _logger.LogWarning(nameof(OnStart) + (param??string.Empty));
        }

        protected override void OnStop()
        {
            _logger.LogInformation(nameof(OnStop));
        }

        protected override void OnException(Exception ex)
        {
            _logger.LogError(ex, nameof(OnException) + (ex.Data["Method"]??string.Empty) );
        }
    }
}

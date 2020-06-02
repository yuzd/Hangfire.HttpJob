using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestSqlserverHangfireAgent.Jobs
{
    public class TestJob : JobAgent
    {
        private readonly ILogger<TestJob> _logger;

        public TestJob(ILogger<TestJob> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Create {nameof(TestJob)} Instance Success");
        }
        public override async Task OnStart(JobContext jobContext)
        {
            jobContext.Console.WriteLine("开始等待10秒");
            await Task.Delay(1000 * 10);
            jobContext.Console.WriteLine("结束等待10秒");
            jobContext.Console.WriteLine("哈哈哈哈",ConsoleFontColor.Cyan);
            _logger.LogWarning(nameof(OnStart) + (jobContext.Param ?? string.Empty));
        }

        public override void OnStop(JobContext jobContext)
        {
            _logger.LogInformation(nameof(OnStop));
        }

        public override void OnException(JobContext jobContext, Exception ex)
        {
            _logger.LogError(ex, nameof(OnException) + (ex.Data["Method"] ?? string.Empty));
        }
    }
}

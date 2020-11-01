using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Hangfire.HttpJob.Agent.Util;
using Microsoft.Extensions.Logging;

namespace TestHangfireAgent.Jobs
{
    [SingletonJob]//不打这个标签也行 默认就是单例的
    public class TestJob : JobAgent
    {
        public TestJob(ILogger<TestJob> logger)
        {
            logger.LogInformation($"Create {nameof(TestJob)} Instance Success");
        }
        public override async Task OnStart(JobContext jobContext)
        {
            jobContext.Console.WriteLine("开始等待1秒");
            await Task.Delay(1000 * 1);
            jobContext.Console.WriteLine("结束等待1秒");
            jobContext.Console.WriteLine("开始测试Progressbar",ConsoleFontColor.Cyan);

            var bar = jobContext.Console.WriteProgressBar("testbar");
            for (int i = 0; i < 10; i++)
            {
                bar.SetValue(i * 10);
                await Task.Delay(1000);
            }
        }


    }
}

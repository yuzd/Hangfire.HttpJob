﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.Logging;

namespace TestSqlserverHangfireAgent.Jobs
{
    [SingletonJob(RegisterName = "单例job")]//不打这个标签也行 默认就是单例的
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
            jobContext.Console.Info("info消息");
            jobContext.Console.Warning("waring消息");
            jobContext.Console.Error("error消息");
            jobContext.Console.Info("开始等待1秒");
            await Task.Delay(1000 * 1);
            jobContext.Console.Info("结束等待1秒");
            jobContext.Console.WriteLine("开始测试Progressbar", ConsoleFontColor.Cyan);

            var bar = jobContext.Console.WriteProgressBar("testbar");
            for (int i = 0; i < 10; i++)
            {
                bar.SetValue(i * 10);
                await Task.Delay(1000);
            }
        }


    }
}

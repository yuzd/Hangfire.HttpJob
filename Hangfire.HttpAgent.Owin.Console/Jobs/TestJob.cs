using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Hangfire.HttpAgent.Owin.Cons
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
            jobContext.Console.WriteLine("开始等待1秒");
            await Task.Delay(1000 * 1);
            jobContext.Console.WriteLine("结束等待1秒");
            jobContext.Console.WriteLine("开始测试Progressbar", ConsoleFontColor.Cyan);

            var bar = jobContext.Console.WriteProgressBar("testbar");
            for (int i = 0; i < 30; i++)
            {
                bar.SetValue(i * 100 / 30);
                await Task.Delay(1000);
            }
        }


    }
}

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hangfire.HttpJob.Client.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestAddBackgroundJob()
        {
            var serverUrl = "http://localhost:5000/job";
            var result = HangfireJobClient.AddBackgroundJob(serverUrl, new BackgroundJob
            {
                JobName = "≤‚ ‘api",
                Method = "Get",
                Url = "http://localhost:5000/testaaa",
                Mail = new List<string> {"1877682825@qq.com"},
                SendSucMail = true,
                DelayFromMinutes = 1,
                Success = new HttpCallbackJob
                {
                    Method = "Get",
                    Url = "http://localhost:5000/testSuccess",
                    Success  = new HttpCallbackJob
                    {
                        Method = "Get",
                        Url = "http://localhost:5000/testSuccess",
                    },
                    Fail = new HttpCallbackJob
                    {
                        Method = "Get",
                        Url = "http://localhost:5000/testFail"
                    }
                },
                Fail = new HttpCallbackJob
                {
                    Method = "Get",
                    Url = "http://localhost:5000/testFail",
                    Success  = new HttpCallbackJob
                    {
                        Method = "Get",
                        Url = "http://localhost:5000/testSuccess",
                    },
                    Fail = new HttpCallbackJob
                    {
                        Method = "Get",
                        Url = "http://localhost:5000/testFail"
                    }
                }
            }, new HangfireServerPostOption
            {
                BasicUserName = "admin",
                BasicPassword = "test"
            });
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.JobId!=null);
            TestRemoveBackgroundJob(result.JobId);

        }

        [TestMethod]
        public void TestAddRecurringJob()
        {
            var serverUrl = "http://localhost:5000/job";
            var result = HangfireJobClient.AddRecurringJob(serverUrl, new RecurringJob()
            {
                JobName = "≤‚ ‘5µ„40÷¥––",
                Method = "Post",
                Data = new {name = "aaa",age = 10},
                Url = "http://localhost:5000/testpost",
                Mail = new List<string> { "1877682825@qq.com" },
                SendSucMail = true,
                Cron = "40 17 * * *"
            }, new HangfireServerPostOption
            {
                BasicUserName = "admin",
                BasicPassword = "test"
            });
            Assert.IsTrue(result.IsSuccess);
        }

        public void TestRemoveBackgroundJob(string jobId)
        {
            var serverUrl = "http://localhost:5000/job";
            var result = HangfireJobClient.RemoveBackgroundJob(serverUrl, jobId, new HangfireServerPostOption
            {
                BasicUserName = "admin",
                BasicPassword = "test"
            });
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestRemoveRecurringJob()
        {
            var serverUrl = "http://localhost:5000/job";
            var result = HangfireJobClient.RemoveRecurringJob(serverUrl, "≤‚ ‘5µ„40÷¥––", new HangfireServerPostOption
            {
                BasicUserName = "admin",
                BasicPassword = "test"
            });
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void TestAddBackgroundJobWithHeaders()
        {
            var serverUrl = "http://localhost:5000/job";
            var result = HangfireJobClient.AddBackgroundJob(serverUrl, new BackgroundJob
            {
                JobName = "≤‚ ‘api",
                Method = "Get",
                Url = "http://localhost:5000/testaaa",
                Mail = new List<string> { "1877682825@qq.com" },
                SendSucMail = true,
                DelayFromMinutes = 1,
                Headers = new Dictionary<string, string>
                {
                    {"token" , "aaaa" }
                }
            }, new HangfireServerPostOption
            {
                BasicUserName = "admin",
                BasicPassword = "test"
            });
            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.JobId != null);
        }
    }
}

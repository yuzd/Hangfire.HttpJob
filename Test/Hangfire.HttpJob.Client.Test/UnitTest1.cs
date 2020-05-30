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
                JobName = "测试api",
                Method = "Get",
                Url = "http://localhost:5000/testaaa",
                Mail = new List<string> {"1877682825@qq.com"},
                SendSuccess = true,
                DelayFromMinutes = 1,
                TimeZone = "",
                DingTalk = new DingTalkOption
                {
                    Token = "",
                    AtPhones = "",
                    IsAtAll = false
                },
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
                JobName = "测试5点40执行",
                Method = "Post",
                Data = new {name = "aaa",age = 10},
                Url = "http://localhost:5000/testpost",
                Mail = new List<string> { "1877682825@qq.com" },
                SendSuccess = true,
                Cron = "40 17 * * *",
                TimeZone = "",
                DingTalk = new DingTalkOption
                {
                    Token = "",
                    AtPhones = "",
                    IsAtAll = false
                },
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
            var result = HangfireJobClient.RemoveRecurringJob(serverUrl, "测试5点40执行", new HangfireServerPostOption
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
                JobName = "测试api",
                Method = "Get",
                Url = "http://localhost:5000/testaaa",
                Mail = new List<string> { "1877682825@qq.com" },
                SendSuccess = true,
                DelayFromMinutes = 1,
                QueueName = "APIS",
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

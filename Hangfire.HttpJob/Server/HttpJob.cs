using Hangfire.Console;
using Hangfire.HttpJob.Content.resx;
using Hangfire.HttpJob.Support;
using Hangfire.Logging;
using Hangfire.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Hangfire.HttpJob.Server
{
    internal class HttpJob
    {
        #region Field

        private static readonly ILog Logger = LogProvider.For<HttpJob>();
        public static HangfireHttpJobOptions HangfireHttpJobOptions;

        #endregion

        #region Public

        /// <summary>
        /// 发起HTTP调度
        /// </summary>
        /// <param name="item">job详情</param>
        /// <param name="jobName">job名称</param>
        /// <param name="queuename">指定queue名称(Note: Hangfire queue names need to be lower case)</param>
        /// <param name="isretry">是否http调用出错重试</param>
        /// <param name="context">上下文</param>
        [AutomaticRetrySet(Attempts = 3, DelaysInSeconds = new[] {20, 30, 60}, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisplayName("[{1} | {2} | Retry:{3}]")]
        [JobFilter(timeoutInSeconds: 3600)]
        public static void Excute(HttpJobItem item, string jobName = null, string queuename = null, bool isretry = false, PerformContext context = null)
        {
            var logList = new List<string>();
            var result = false;
            try
            {
                context.Items.TryGetValue("Data", out var runTimeDataItem);
                var runTimeData = runTimeDataItem as string;
                if (!string.IsNullOrEmpty(runTimeData))
                {
                    item.Data = runTimeData;
                }

                if (Run(item, context, logList))
                {
                    SendSuccessMail(item, string.Join("<br/>", logList));
                    result = true;
                }
                else
                {
                    SendFailMail(item, string.Join("<br/>", logList),null);
                    result = false;
                }
               
            }
            catch (Exception ex)
            {
                RunWithTry(() => context.SetTextColor(ConsoleTextColor.Red));
                Logger.ErrorException("HttpJob.Excute=>" + item, ex);
                RunWithTry(() => context.WriteLine(ex.ToString()));
                if (!item.EnableRetry)
                {
                    if (item.Fail != null)
                    {
                        if (Run(item.Fail, context, logList,item))
                        {
                            SendSuccessMail(item, string.Join("<br/>", logList));
                            return;
                        }
                    }
                    SendFailMail(item, string.Join("<br/>", logList), ex);
                    AddErrToJob(context, ex);
                    return;
                }

                //获取重试次数
                var count = RunWithTry<string>(() => context.GetJobParameter<string>("RetryCount")) ?? string.Empty;
                if (count == "3") //重试达到三次的时候发邮件通知
                {
                    if (item.Fail != null)
                    {
                        if (Run(item.Fail, context, logList,item))
                        {
                            SendSuccessMail(item, string.Join("<br/>", logList));
                            return;
                        }
                    }
                    RunWithTry(() => context.WriteLine(Strings.LimitReached));
                    logList.Add(Strings.LimitReached);
                    SendFailMail(item, string.Join("<br/>", logList), ex);
                    AddErrToJob(context, ex);
                    return;
                }

                context.Items.Add("RetryCount", count);
                throw;
            }

            if (!result)
            {
                throw new ChildJobException("Child job Fail");
            }
        }

        /// <summary>
        /// Run HttpRequest
        /// </summary>
        /// <param name="item"></param>
        /// <param name="context"></param>
        /// <param name="logList"></param>
        /// <param name="parentJob"></param>
        /// <exception cref="HttpStatusCodeException"></exception>
        private static bool Run(HttpJobItem item, PerformContext context, List<string> logList, HttpJobItem parentJob = null)
        {
            try
            {
                if (parentJob == null && item.Timeout < 1) item.Timeout = 5000;
                if (parentJob != null)
                {
                    RunWithTry(() => context.SetTextColor(ConsoleTextColor.Green));
                    if(item.Timeout<1) item.Timeout = parentJob.Timeout;
                    if(item.Data.Contains("@parent@")) item.Data = item.Data.Replace("@parent@",parentJob.Cron);
                    if (string.IsNullOrEmpty(item.BasicUserName)) item.BasicUserName = parentJob.BasicUserName;
                    if (string.IsNullOrEmpty(item.BasicPassword)) item.BasicPassword = parentJob.BasicPassword;
                    if (item.Headers == null || !item.Headers.Any()) item.Headers = parentJob.Headers;
                    if (string.IsNullOrEmpty(item.QueueName)) item.QueueName = parentJob.QueueName;
                }
                else
                {
                    RunWithTry(() => context.SetTextColor(ConsoleTextColor.Yellow));
                }
                
               
                RunWithTry(() => context.WriteLine($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                logList.Add($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                RunWithTry(() =>
                    context.WriteLine(
                        $"{Strings.JobName}:{item.JobName ?? string.Empty}|{Strings.QueuenName}:{(string.IsNullOrEmpty(item.QueueName) ? "DEFAULT" : item.QueueName)}"));
                logList.Add(
                    $"{Strings.JobName}:{item.JobName ?? string.Empty}|{Strings.QueuenName}:{(string.IsNullOrEmpty(item.QueueName) ? "DEFAULT" : item.QueueName)}");
                RunWithTry(() => context.WriteLine($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item)}】"));
                logList.Add($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item, Formatting.Indented)}】");
                HttpClient client;
                if (!string.IsNullOrEmpty(HangfireHttpJobOptions.Proxy))
                {
                    // per proxy per HttpClient
                    client = HangfireHttpClientFactory.Instance.GetProxiedHttpClient(HangfireHttpJobOptions.Proxy);
                    RunWithTry(() => context.WriteLine($"Proxy:{HangfireHttpJobOptions.Proxy}"));
                    logList.Add($"Proxy:{HangfireHttpJobOptions.Proxy}");
                }
                else
                {
                    //per host per HttpClient
                    client = HangfireHttpClientFactory.Instance.GetHttpClient(item.Url);
                }

                var httpMesage = PrepareHttpRequestMessage(item, context);
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(item.Timeout));
                var httpResponse = client.SendAsync(httpMesage, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                HttpContent content = httpResponse.Content;
                string result = content.ReadAsStringAsync().GetAwaiter().GetResult();
                RunWithTry(() => context.WriteLine($"{Strings.ResponseCode}:{httpResponse.StatusCode}"));
                logList.Add($"{Strings.ResponseCode}:{httpResponse.StatusCode}");

                //检查HttpResponse StatusCode
                if (HangfireHttpJobOptions.CheckHttpResponseStatusCode(httpResponse.StatusCode))
                {
                    RunWithTry(() => context.WriteLine($"{Strings.ResponseCode}:{httpResponse.StatusCode} ===> CheckResult: Ok "));
                    logList.Add($"{Strings.ResponseCode}:{httpResponse.StatusCode} ===> CheckResult: Ok ");
                }
                else
                {
                    throw new HttpStatusCodeException(httpResponse.StatusCode);
                }

                RunWithTry(() => context.WriteLine($"{Strings.JobResult}:{result}"));
                logList.Add($"{Strings.JobResult}:{result}");
                RunWithTry(() => context.WriteLine($"{Strings.JobEnd}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                logList.Add($"{Strings.JobEnd}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                //到这里查看是否有 子Job
                if (item.Success != null)
                {
                    item.Cron = result;
                    return Run(item.Success, context, logList, item);
                }

                return true;
            }
            catch (Exception e)
            {
                if (parentJob==null) throw;
                
                RunWithTry(() => context.SetTextColor(ConsoleTextColor.Red));
                Logger.ErrorException("HttpJob.Excute=>" + item, e);
                RunWithTry(() => context.WriteLine(e.ToString()));
                
                //到这里查看是否有 子Job
                if (item.Fail != null)
                {
                    item.Cron = e.Message;
                    return Run(item.Fail, context, logList, item);
                }
                return false;
            }
        }


        /// <summary>
        /// 获取AgentJob的运行详情
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string GetAgentJobDetail(HttpJobItem item)
        {
            if (item.Timeout < 1) item.Timeout = 5000;
            HttpClient client;
            if (!string.IsNullOrEmpty(HangfireHttpJobOptions.Proxy))
            {
                // per proxy per HttpClient
                client = HangfireHttpClientFactory.Instance.GetProxiedHttpClient(HangfireHttpJobOptions.Proxy);
            }
            else
            {
                //per host per HttpClient
                client = HangfireHttpClientFactory.Instance.GetHttpClient(item.Url);
            }

            var request = new HttpRequestMessage(new HttpMethod("Get"), item.Url);
            request.Headers.Add("x-job-agent-class", item.AgentClass);
            request.Headers.Add("x-job-agent-action", "detail");
            if (!string.IsNullOrEmpty(item.BasicUserName) && !string.IsNullOrEmpty(item.BasicPassword))
            {
                var byteArray = Encoding.ASCII.GetBytes(item.BasicUserName + ":" + item.BasicPassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }


            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(item.Timeout));
            var httpResponse = client.SendAsync(request, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            HttpContent content = httpResponse.Content;
            string result = content.ReadAsStringAsync().GetAwaiter().GetResult();
            return result;
        }

        #endregion

        #region Private

        private static void Complete()
        {
        }

        private static void SendSuccessMail(HttpJobItem item, string result)
        {
            try
            {
                if (!item.SendSucMail) return;
                var mail = string.IsNullOrEmpty(item.Mail)
                    ? string.Join(",", HangfireHttpJobOptions.MailOption.AlertMailList)
                    : item.Mail;

                if (string.IsNullOrWhiteSpace(mail)) return;
                var subject = $"【JOB】[Success]" + item.JobName;
                result = result.Replace("\n", "<br/>");
                result = result.Replace("\r\n", "<br/>");
                EmailService.Instance.Send(mail, subject, result);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJob.SendSuccessMail=>" + item, ex);
            }
        }


        private static void SendFailMail(HttpJobItem item, string result, Exception exception)
        {
            try
            {
                if (!item.SendFaiMail) return;
                var mail = string.IsNullOrEmpty(item.Mail)
                    ? string.Join(",", HangfireHttpJobOptions.MailOption.AlertMailList)
                    : item.Mail;

                if (string.IsNullOrWhiteSpace(mail)) return;
                var subject = $"【JOB】[Fail]" + item.JobName;
                result = result.Replace("\n", "<br/>");
                result = result.Replace("\r\n", "<br/>");
                if (exception != null)
                {
                    result += BuildExceptionMsg(exception);
                }

                EmailService.Instance.Send(mail, subject, result);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("HttpJob.SendFailMail=>" + item, ex);
            }
        }

        private static string BuildExceptionMsg(Exception ex, string prefix = "")
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(GetHtmlFormat(ex.GetType().ToString()));
                sb.AppendLine("Messgae:" + GetHtmlFormat(ex.Message));
                sb.AppendLine("StackTrace:<br/>" + GetHtmlFormat(ex.StackTrace));
                if (ex.InnerException != null)
                {
                    sb.AppendLine(BuildExceptionMsg(ex.InnerException, prefix + "&nbsp;&nbsp;&nbsp;"));
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string GetHtmlFormat(string v)
        {
            return v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static HttpRequestMessage PrepareHttpRequestMessage(HttpJobItem item, PerformContext context)
        {
            var request = new HttpRequestMessage(new HttpMethod(item.Method), item.Url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(item.ContentType));
            if (!item.Method.ToLower().Equals("get"))
            {
                if (!string.IsNullOrEmpty(item.Data))
                {
                    //var bytes = Encoding.UTF8.GetBytes(item.Data);
                    request.Content = new StringContent(item.Data, Encoding.UTF8, item.ContentType);
                    //request.Content = new ByteArrayContent(bytes, 0, bytes.Length);
                }
            }

            var headerKeys = string.Empty;
            if (item.Headers != null && item.Headers.Count > 0)
            {
                foreach (var header in item.Headers)
                {
                    if (string.IsNullOrEmpty(header.Key)) continue;
                    request.Headers.Add(header.Key, header.Value);
                }

                headerKeys = string.Join("；", item.Headers.Keys);
            }

            if (!string.IsNullOrEmpty(item.AgentClass))
            {
                request.Headers.Add("x-job-agent-class", item.AgentClass);
                if (!string.IsNullOrEmpty(headerKeys))
                {
                    request.Headers.Add("x-job-agent-header", headerKeys);
                }

                var consoleInfo = GetConsoleInfo(context);
                if (consoleInfo != null)
                {
                    request.Headers.Add("x-job-agent-console", JsonConvert.SerializeObject(consoleInfo));
                }
            }

            if (context != null)
            {
                context.Items.TryGetValue("Action", out var actionItem);
                var action = actionItem as string;
                if (!string.IsNullOrEmpty(action))
                {
                    request.Headers.Add("x-job-agent-action", action);
                }
                else if (!string.IsNullOrEmpty(item.AgentClass))
                {
                    request.Headers.Add("x-job-agent-action", "run");
                }
            }

            if (!string.IsNullOrEmpty(item.BasicUserName) && !string.IsNullOrEmpty(item.BasicPassword))
            {
                var byteArray = Encoding.ASCII.GetBytes(item.BasicUserName + ":" + item.BasicPassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            return request;
        }

        /// <summary>
        /// AgentJob的话 取得Console的参数
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static ConsoleInfo GetConsoleInfo(PerformContext context)
        {
            try
            {
                if (context == null)
                {
                    // PerformContext might be null because of refactoring, or during tests
                    return null;
                }

                if (!context.Items.ContainsKey("ConsoleContext"))
                {
                    // Absence of ConsoleContext means ConsoleServerFilter was not properly added
                    return null;
                }

                var consoleContext = context.Items["ConsoleContext"];

                //反射获取私有属性 _consoleId

                var consoleValue = consoleContext?.GetType().GetField("_consoleId", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(consoleContext);

                if (consoleValue == null) return null;

                //反射获取ConsoleId的私有属性 DateValue 值

                var dateValue = consoleValue.GetType().GetProperty("DateValue", BindingFlags.Instance | BindingFlags.Public)?.GetValue(consoleValue);

                return new ConsoleInfo
                {
                    HashKey = $"console:refs:{consoleValue}",
                    SetKey = $"console:{consoleValue}",
                    StartTime = (DateTime?) dateValue ?? DateTime.Now
                };
            }
            catch (Exception)
            {
                return null;
            }
        }


        private static T RunWithTry<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                Logger.ErrorException("RunWithTry", e);
            }

            return default(T);
        }

        private static void RunWithTry(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logger.ErrorException("RunWithTry", e);
            }
        }

        private static void AddErrToJob(PerformContext context, Exception ex)
        {
            context.SetJobParameter("jobErr", ex.Message);
        }

        #endregion
    }
}
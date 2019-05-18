using Hangfire.Console;
using Hangfire.HttpJob.Content.resx;
using Hangfire.HttpJob.Support;
using Hangfire.Logging;
using Hangfire.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Hangfire.HttpJob.Server
{
    internal class HttpJob
    {
        #region Field
        private static readonly ILog Logger = LogProvider.For<HttpJob>();
        public static HangfireHttpJobOptions HangfireHttpJobOptions;

        #endregion

        #region Public


        [AutomaticRetrySet(Attempts = 3, DelaysInSeconds = new[] { 20, 30, 60 }, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisplayName("[JobName:{1} | QueueName:{2} | EnableRetry:{3}]")]
        public static void Excute(HttpJobItem item, string jobName = null, string queuename = null, bool isretry = false, PerformContext context = null)
        {
            var logList = new List<string>();
            try
            {
                context.SetTextColor(ConsoleTextColor.Yellow);
                context.WriteLine($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                logList.Add($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                context.WriteLine($"{Strings.JobName}:{jobName ?? string.Empty}|{Strings.QueuenName}:{queuename ?? "DEFAULT"}");
                logList.Add($"{Strings.JobName}:{jobName ?? string.Empty}|{Strings.QueuenName}:{queuename ?? "DEFAULT"}");
                context.WriteLine($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item)}】");
                logList.Add($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item, Formatting.Indented)}】");
                var client = item.InitHttpClient();
                var httpMesage = PrepareHttpRequestMessage(item);
                var httpResponse = client.SendAsync(httpMesage).GetAwaiter().GetResult();
                HttpContent content = httpResponse.Content;
                string result = content.ReadAsStringAsync().GetAwaiter().GetResult();
                context.WriteLine($"{Strings.ResponseCode}:{httpResponse.StatusCode}");
                logList.Add($"{Strings.ResponseCode}:{httpResponse.StatusCode}");
                context.WriteLine($"{Strings.JobResult}:{result}");
                logList.Add($"{Strings.JobResult}:{result}");
                SendSuccessMail(item, string.Join("<br/>", logList));
            }
            catch (Exception ex)
            {
                if (context == null) return;
                //获取重试次数
                var count = context.GetJobParameter<string>("RetryCount");
                context.SetTextColor(ConsoleTextColor.Red);
                if (count == "3" && item.SendFaiMail)//重试达到三次的时候发邮件通知
                {
                    SendFailMail(item, string.Join("<br/>", logList), ex);
                }
                Logger.ErrorException("HttpJob.Excute=>" + item, ex);
                context.WriteLine(ex.Message);
                throw;
            }
        }


        #endregion

        #region Private

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
        private static string GetHtmlFormat(string v)
        {
            return v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static HttpRequestMessage PrepareHttpRequestMessage(HttpJobItem item)
        {
            var request = new HttpRequestMessage(new HttpMethod(item.Method), item.Url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(item.ContentType));
            if (!item.Method.ToLower().Equals("get"))
            {
                if (!string.IsNullOrEmpty(item.Data))
                {
                    var bytes = Encoding.UTF8.GetBytes(item.Data);
                    request.Content = new ByteArrayContent(bytes, 0, bytes.Length);
                }
            }
            if (!string.IsNullOrEmpty(item.BasicUserName) && !string.IsNullOrEmpty(item.BasicPassword))
            {
                var byteArray = Encoding.ASCII.GetBytes(item.BasicUserName + ":" + item.BasicPassword);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            return request;
        }
        #endregion
    }



}

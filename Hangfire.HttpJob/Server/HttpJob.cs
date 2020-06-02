using Hangfire.Console;
using Hangfire.HttpJob.Content.resx;
using Hangfire.HttpJob.Support;
using Hangfire.Logging;
using Hangfire.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Spring.Expressions;

namespace Hangfire.HttpJob.Server
{
    internal class HttpJob
    {
        #region Field

        private static readonly ILog Logger = LogProvider.For<HttpJob>();
      
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
        [AutomaticRetrySet(Attempts = 3, DelaysInSeconds = new[] { 20, 30, 60 }, LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
        [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        [DisplayName("[{1} | {2} | Retry:{3}]")]
        [JobFilter(timeoutInSeconds: 3600)]
        public static void Excute(HttpJobItem item, string jobName = null, string queuename = null, bool isretry = false, PerformContext context = null)
        {
            var logList = new List<string>();
            var result = false;
            try
            {
                object runTimeDataItem = null;
                context?.Items.TryGetValue("Data", out runTimeDataItem);
                if(runTimeDataItem!=null)
                {
                    var runTimeData = runTimeDataItem as string;
                    if (!string.IsNullOrEmpty(runTimeData))
                    {
                        item.Data = runTimeData;
                    }
                }

                if (Run(item, context, logList))
                {
                    SendSuccess(context.BackgroundJob.Id, item, string.Join("<br/>", logList));
                    result = true;
                }
                else
                {
                    SendFail(context.BackgroundJob.Id, item, string.Join("<br/>", logList), null);
                }
            }
            catch (Exception ex)
            {
                var statusCodeEx = ex as HttpStatusCodeException;
                RunWithTry(() => context.SetTextColor(ConsoleTextColor.Red));
                Logger.ErrorException("HttpJob.Excute=>" + item, ex);
                if (statusCodeEx != null && statusCodeEx.IsEl)
                {
                    RunWithTry(() => context.WriteLine($"【{Strings.CallbackELExcuteResult}:Fail 】" + statusCodeEx.El));
                }
                else
                {
                    RunWithTry(() => context.WriteLine(ex.ToString()));
                }

                if (!item.EnableRetry)
                {
                    if (item.Fail != null)
                    {
                        item.Fail.CallbackRoot = item.JobName + ".Fail";
                        item.Cron = statusCodeEx == null || string.IsNullOrEmpty(statusCodeEx.Msg) ? ex.Message : statusCodeEx.Msg;
                        if (Run(item.Fail, context, logList, item))
                        {
                            SendSuccess(context.BackgroundJob.Id, item, string.Join("<br/>", logList));
                            return;
                        }
                    }
                    SendFail(context.BackgroundJob.Id, item, string.Join("<br/>", logList), ex);
                    AddErrToJob(context, ex);
                    throw;
                }

                //获取重试次数
                var count = RunWithTry<string>(() => context.GetJobParameter<string>("RetryCount")) ?? string.Empty;
                if (count == "3") //重试达到三次的时候发邮件通知
                {
                    if (item.Fail != null)
                    {
                        item.Fail.CallbackRoot = item.JobName + ".Fail";
                        item.Cron = statusCodeEx == null || string.IsNullOrEmpty(statusCodeEx.Msg) ? ex.Message : statusCodeEx.Msg;
                        if (Run(item.Fail, context, logList, item))
                        {
                            SendSuccess(context.BackgroundJob.Id, item, string.Join("<br/>", logList));
                            return;
                        }
                    }
                    RunWithTry(() => context.WriteLine(Strings.LimitReached));
                    logList.Add(Strings.LimitReached);
                    SendFail(context.BackgroundJob.Id, item, string.Join("<br/>", logList), ex);
                    AddErrToJob(context, ex);
                    return;
                }

                context?.Items.Add("RetryCount", count);
                throw;
            }

            if (!result)
            {
                throw new CallbackJobException("Callback job Fail");
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
            CancellationTokenSource cancelToken = null;
            try
            {
                if (parentJob != null)
                {
                    RunWithTry(() => context.SetTextColor(ConsoleTextColor.Green));
                    if (item.Timeout < 1) item.Timeout = parentJob.Timeout;
                    if (item.Data.Contains("@parent@")) item.Data = item.Data.Replace("@parent@", parentJob.Cron);
                    if (string.IsNullOrEmpty(item.BasicUserName)) item.BasicUserName = parentJob.BasicUserName;
                    if (string.IsNullOrEmpty(item.BasicPassword)) item.BasicPassword = parentJob.BasicPassword;
                    if (item.Headers == null || !item.Headers.Any()) item.Headers = parentJob.Headers;
                    if (string.IsNullOrEmpty(item.QueueName)) item.QueueName = parentJob.QueueName;
                    RunWithTry(() => context.WriteLine($"【{Strings.CallbackStart}】[{item.CallbackRoot}]"));
                    item.JobName = item.CallbackRoot;
                }
                else
                {
                    if (string.IsNullOrEmpty(item.CallbackRoot)) item.CallbackRoot = item.JobName;
                    if (item.Timeout < 1) item.Timeout = 5000;
                    RunWithTry(() => context.SetTextColor(ConsoleTextColor.Yellow));
                }


                RunWithTry(() => context.WriteLine($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                logList.Add($"{Strings.JobStart}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                RunWithTry(() => context.WriteLine($"{Strings.JobName}:{item.JobName ?? string.Empty}|{Strings.QueuenName}:{(string.IsNullOrEmpty(item.QueueName) ? "DEFAULT" : item.QueueName)}"));
                logList.Add(
                    $"{Strings.JobName}:{item.JobName ?? string.Empty}|{Strings.QueuenName}:{(string.IsNullOrEmpty(item.QueueName) ? "DEFAULT" : item.QueueName)}");
                RunWithTry(() => context.WriteLine($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item)}】"));
                logList.Add($"{Strings.JobParam}:【{JsonConvert.SerializeObject(item, Formatting.Indented)}】");
                HttpClient client;
                
                //当前job指定如果开启了proxy 并且 有配置代理 那么就走代理
                if (CodingUtil.TryGetGlobalProxy(out var globalProxy) && item.Headers != null && item.Headers.TryGetValue("proxy", out var enableCurrentJobProxy) && !string.IsNullOrEmpty(enableCurrentJobProxy) && enableCurrentJobProxy.ToLower().Equals("true"))
                {
                    // per proxy per HttpClient
                    client = HangfireHttpClientFactory.Instance.GetProxiedHttpClient(globalProxy);
                    RunWithTry(() => context.WriteLine($"Use Proxy:{globalProxy}"));
                    logList.Add($"Proxy:{globalProxy}");
                }
                else
                {
                    //per host per HttpClient
                    client = HangfireHttpClientFactory.Instance.GetHttpClient(item.Url);
                }

                var httpMesage = PrepareHttpRequestMessage(item, context, parentJob);
                cancelToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(item.Timeout));
                var httpResponse = client.SendAsync(httpMesage, cancelToken.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                HttpContent content = httpResponse.Content;
                string result = content.ReadAsStringAsync().GetAwaiter().GetResult();
                RunWithTry(() => context.WriteLine($"{Strings.ResponseCode}:{httpResponse.StatusCode}"));
                logList.Add($"{Strings.ResponseCode}:{httpResponse.StatusCode}");
                RunWithTry(() => context.WriteLine($"{Strings.JobResult}:{result}"));
                logList.Add($"{Strings.JobResult}:{result}");
                RunWithTry(() => context.WriteLine($"{Strings.JobEnd}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}"));
                logList.Add($"{Strings.JobEnd}:{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                //如果agent那边调度报错
                if (CodingUtil.HangfireHttpJobOptions.EnableJobAgentErrorThrow && !string.IsNullOrEmpty(item.AgentClass) && httpResponse.StatusCode ==  HttpStatusCode.InternalServerError)
                {
                    throw new AgentJobException(item.AgentClass,result);
                }
                //检查HttpResponse StatusCode
                else if (CodingUtil.HangfireHttpJobOptions.CheckHttpResponseStatusCode(httpResponse.StatusCode, result))
                {
                    RunWithTry(() => context.WriteLine($"{Strings.ResponseCode}:{httpResponse.StatusCode} ===> CheckResult: Ok "));
                    logList.Add($"{Strings.ResponseCode}:{httpResponse.StatusCode} ===> CheckResult: Ok ");
                }
                else
                {
                    //错误的log都会在exception里面出
                    throw new HttpStatusCodeException(httpResponse.StatusCode, result);
                }

                //检查是否有设置EL表达式 可以自定义检查StatusCode 和 解析返回的参数值
                if (!string.IsNullOrEmpty(item.CallbackEL))
                {
                    var elResult = InvokeSpringElCondition(item.CallbackEL, result, context,
                        new Dictionary<string, object> { { "resultBody", result } , { "StatusCode", (int)httpResponse.StatusCode } });
                    if (!elResult)
                    {
                        //错误的log都会在exception里面出
                        throw new HttpStatusCodeException(item.CallbackEL, result);
                    }

                    RunWithTry(() => context.WriteLine($"【{Strings.CallbackELExcuteResult}:Ok 】" + item.CallbackEL));
                }

               
                if (parentJob != null)
                    RunWithTry(() => context.WriteLine($"【{Strings.CallbackSuccess}】[{item.CallbackRoot}]"));

                //到这里查看是否有 子Job
                if (item.Success != null)
                {
                    item.Cron = result; //父job的执行结果
                    item.Success.CallbackRoot = item.CallbackRoot + ".Success";
                    return Run(item.Success, context, logList, item);
                }

                return true;
            }
            catch (Exception e)
            {
                RunWithTry(() => context.SetTextColor(ConsoleTextColor.Red));
                if (cancelToken != null && cancelToken.IsCancellationRequested)
                {
                    //说明是被我们自己设置的timeout超时了
                    RunWithTry(() => context.WriteLine("【HttpJob Timeout】：" + item.Timeout + "ms"));
                }

                if (parentJob == null)
                {
                    throw;
                }

                //走到这里应该都是callback的抛出的异常了
                Logger.ErrorException("HttpJob.Excute=>" + item, e);
                RunWithTry(() => context.WriteLine($"【{Strings.CallbackFail}】[{item.CallbackRoot}]"));
                if (e is HttpStatusCodeException exception && exception.IsEl)
                {
                    RunWithTry(() => context.WriteLine($"【{Strings.CallbackELExcuteResult}:Fail 】" + exception.El));
                }
                else
                {
                    RunWithTry(() => context.WriteLine(e.ToString()));
                }

                //到这里查看是否有 子Job
                if (item.Fail != null)
                {
                    item.Cron = e.Message;//父job的执行异常堆栈
                    item.Fail.CallbackRoot = item.CallbackRoot + ".Fail";
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
            if (!string.IsNullOrEmpty(CodingUtil.HangfireHttpJobOptions.Proxy))
            {
                // per proxy per HttpClient
                client = HangfireHttpClientFactory.Instance.GetProxiedHttpClient(CodingUtil.HangfireHttpJobOptions.Proxy);
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

        
        /// <summary>
        /// 发送钉钉通知
        /// </summary>
        private static void SendDingTalkNotice(HttpJobItem item,string jobId, string resString,bool isSuccess, Exception exception = null)
        {
            try
            {
                //成功 并且开启了 成功通知 才进行钉钉播报
                if (isSuccess && !item.SendSuccess)
                {
                    return;
                }

                //失败 并且开启了 失败通知 才进行钉钉播报
                if (!isSuccess && !item.SendFail)
                {
                    return;
                }

                DingTalkOption dingTalk = item.DingTalk ?? CodingUtil.HangfireHttpJobOptions.DingTalkOption;
                if (dingTalk == null || string.IsNullOrEmpty(dingTalk.Token))
                {
                    return;
                }

                //优先使用全局配置里面的参数
                CodingUtil.GetGlobalAppsettings().TryGetValue("CurrentDomain", out var currentDomain);

                var logDetail = currentDomain!=null && !string.IsNullOrEmpty(currentDomain.ToString())? $"{currentDomain}/job/jobs/details/{jobId}":  string.IsNullOrEmpty(CodingUtil.HangfireHttpJobOptions.CurrentDomain) ? $"JobId:{jobId}" : $"{CodingUtil.HangfireHttpJobOptions.CurrentDomain}/job/jobs/details/{jobId}";

                var content =
                    $@"## {item.JobName} {(isSuccess?"Success": "<font color=#E74C3C>Failed</font>")}{Strings.DingTalkTitle}
### {Strings.DingTalkConfig}
>#### {Strings.QueuenName}:{(string.IsNullOrEmpty(item.QueueName)?"DEFAULT": item.QueueName)} 
### {Strings.DingTalkRequestUrl}: 
> #### {item.Url}
### {Strings.DingTalkResponse}:
>#### {resString}   
### {Strings.DingTalkLogDetail}：
>#### {logDetail}{(exception!=null?"\n\n"+exception.ToString():"")}    
";

                var title = $"{Strings.DingTalkTitle}";

                var obj = new
                {
                    msgtype = "markdown",
                    markdown = new
                    {
                        title,
                        text = content
                    },
                    at = new
                    {
                        atMobiles = dingTalk.AtPhones,
                        isAtAll = dingTalk.IsAtAll,
                    }
                };

                var requestUri = $"https://oapi.dingtalk.com/robot/send?access_token={dingTalk.Token}";
                
                HttpClient httpClient;
                //当前job的钉钉如果开启了proxy 并且 有配置代理 那么就走代理
                if (CodingUtil.TryGetGlobalProxy(out var globalProxy) && item.Headers != null && item.Headers.TryGetValue("dingProxy", out var enableDingProxy) && !string.IsNullOrEmpty(enableDingProxy) && enableDingProxy.ToLower().Equals("true"))
                {
                    // per proxy per HttpClient
                    httpClient = HangfireHttpClientFactory.DingTalkInstance.GetProxiedHttpClient(globalProxy);
                }
                else
                {
                    //per host per HttpClient
                    httpClient = HangfireHttpClientFactory.DingTalkInstance.GetHttpClient(requestUri);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
                {
                    Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json")
                };

                var res = httpClient.SendAsync(request).GetAwaiter().GetResult();
                res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Logger.ErrorException("HttpJob.SendDingTalkNotice=>" + item, e);
            }
        }

        /// <summary>
        /// 发送成功通知
        /// </summary>
        private static void SendSuccess(string jobId,HttpJobItem item, string result)
        {
            new Task(() =>
            {
                SendSuccessMail(item, result);
                SendDingTalkNotice(item, jobId, result,true);
            }).Start();
        }

        /// <summary>
        /// 发送失败通知
        /// </summary>
        private static void SendFail(string jobId, HttpJobItem item, string result, Exception exception)
        {
            new Task(() =>
            {
                SendFailMail(item, result, exception);
                SendDingTalkNotice(item, jobId, result,false,exception);
            }).Start();
        }


        /// <summary>
        /// 发送成功邮件通知
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        private static void SendSuccessMail(HttpJobItem item, string result)
        {
            try
            {
                if (!item.SendSuccess) return;
                var mail = string.IsNullOrEmpty(item.Mail)
                    ? string.Join(",", CodingUtil.HangfireHttpJobOptions.MailOption.AlertMailList)
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

        /// <summary>
        /// 发送失败邮件通知
        /// </summary>
        /// <param name="item"></param>
        /// <param name="result"></param>
        /// <param name="exception"></param>
        private static void SendFailMail(HttpJobItem item, string result, Exception exception)
        {
            try
            {
                if (!item.SendFail) return;
                var mail = string.IsNullOrEmpty(item.Mail)
                    ? string.Join(",", CodingUtil.HangfireHttpJobOptions.MailOption.AlertMailList)
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
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private static string GetHtmlFormat(string v)
        {
            return v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static HttpRequestMessage PrepareHttpRequestMessage(HttpJobItem item, PerformContext context, HttpJobItem parentJob = null)
        {
            var request = new HttpRequestMessage(new HttpMethod(item.Method), item.Url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(item.ContentType));
            if (!item.Method.ToLower().Equals("get"))
            {
                if (!string.IsNullOrEmpty(item.Data))
                {
                    var replaceData = placeHolderCheck(item.Data, parentJob == null ? null : new Dictionary<string, object> { { "parent", parentJob.Cron } });

                    if (replaceData.Item2 != null)
                    {
                        RunWithTry(() => context.WriteLine($"【{Strings.ReplacePlaceHolder}】Error:"));
                        RunWithTry(() => context.WriteLine(replaceData.Item2));
                        request.Content = new StringContent(item.Data, Encoding.UTF8, item.ContentType);
                    }
                    else
                    {
                        if (item.Data.Contains("#{") || item.Data.Contains("${"))
                        {
                            RunWithTry(() => context.WriteLine($"【{Strings.ReplacePlaceHolder}】" + replaceData));
                        }
                        request.Content = new StringContent(replaceData.Item1, Encoding.UTF8, item.ContentType);
                    }
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

                var basicItem = item as BaseJobItems;
                request.Headers.Add("x-job-body",Newtonsoft.Json.JsonConvert.SerializeObject(basicItem));
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
                    StartTime = (DateTime?)dateValue ?? DateTime.Now
                };
            }
            catch (Exception e)
            {
                return null;
            }
        }


        #region PlaceHolder

        private static (string, Exception) placeHolderCheck(string content, Dictionary<string, object> param)
        {
            try
            {
                //先把 ${} 的 placehoder 全部替换掉
                var parameterValue = ResolveEmbeddedValue(content, "${", ResolvePlaceholder);


                if (param != null)
                {
                    var div = new Dictionary<string, object>(CodingUtil.GetGlobalAppsettings());
                    foreach (var keyValuePair in param)
                    {
                        if (div.ContainsKey(keyValuePair.Key))
                        {
                            div[keyValuePair.Key] = keyValuePair.Value;
                            continue;
                        }
                        div.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    var parameterValue2 = ResolveEmbeddedValue(parameterValue, "#{", (str) => ResolveSpringElPlaceholder(str, div));
                    return (parameterValue2, null);
                }

                var parameterValue22 = ResolveEmbeddedValue(parameterValue, "#{", (str) => ResolveSpringElPlaceholder(str, CodingUtil.GetGlobalAppsettings()));
                return (parameterValue22, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        /// <summary>
        /// 替换当前的配置文件
        /// </summary>
        /// <param name="strVal"></param>
        /// <param name="startPrefix"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        private static string ResolveEmbeddedValue(string strVal, string startPrefix, Func<string, string> func)
        {
            string DefaultPlaceholderPrefix = startPrefix;
            string DefaultPlaceholderSuffix = "}";

            int startIndex = strVal.IndexOf(DefaultPlaceholderPrefix, StringComparison.Ordinal);
            while (startIndex != -1)
            {
                int endIndex = strVal.IndexOf(DefaultPlaceholderSuffix, startIndex + DefaultPlaceholderPrefix.Length, StringComparison.Ordinal);
                if (endIndex != -1)
                {
                    int pos = startIndex + DefaultPlaceholderPrefix.Length;
                    string placeholder = strVal.Substring(pos, endIndex - pos);
                    string resolvedValue = func(placeholder);
                    if (resolvedValue != null)
                    {
                        strVal = strVal.Substring(0, startIndex) + resolvedValue.Replace("\"", "\"\"").Replace("\r\n", "").Replace("\r", "").Replace("\n", "") + strVal.Substring(endIndex + 1);
                        startIndex = strVal.IndexOf(DefaultPlaceholderPrefix, startIndex + resolvedValue.Length, StringComparison.Ordinal);
                    }
                    else
                    {
                        return strVal;
                    }
                }
                else
                {
                    startIndex = -1;
                }
            }
            return strVal;
        }

        /// <summary>
        /// 运行SpringEl表达式
        /// </summary>
        /// <param name="placeholder"></param>
        /// <returns></returns>
        private static string ResolvePlaceholder(string placeholder)
        {

            CodingUtil.GetGlobalAppsettings().TryGetValue(placeholder, out var propertyValue);

            if (propertyValue == null)
            {
                propertyValue = Environment.GetEnvironmentVariable(placeholder);
            }
            return propertyValue?.ToString();
        }


        private static string ResolveSpringElPlaceholder(string placeholder, Dictionary<string, object> param)
        {
            var parameterValue = ExpressionEvaluator.GetValue(null, placeholder, param);
            return parameterValue.ToString();
        }


        /// <summary>
        /// 用EL表达式动态判断是否执行成功
        /// </summary>
        /// <returns></returns>
        private static bool InvokeSpringElCondition(string placeholder, string result, PerformContext context, Dictionary<string, object> param)
        {
            try
            {
                try
                {
                    param["result"] = JsonConvert.DeserializeObject<ExpandoObject>(result);
                }
                catch (Exception)
                {
                    //ignore
                }

                var parameterValue = ExpressionEvaluator.GetValue(null, placeholder, param);

                return (bool)parameterValue;
            }
            catch (Exception e)
            {
                context.WriteLine($"【{Strings.CallbackELExcuteError}】" + placeholder);
                context.WriteLine(e);
                return false;
            }
        }

        #endregion

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
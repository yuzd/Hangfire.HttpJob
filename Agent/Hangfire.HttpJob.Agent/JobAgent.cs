using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.HttpJob.Agent.Util;

namespace Hangfire.HttpJob.Agent
{
    public abstract class JobAgent
    {
        private ManualResetEvent _mainThread;
        private volatile int locked;
        /// <summary>
        ///     默认是非Hang
        /// </summary>
        internal volatile bool Hang = false;

        private volatile JobStatus jobStatus = JobStatus.Default;

        /// <summary>
        ///     默认是单例
        /// </summary>
        internal volatile bool Singleton = true;

        /// <summary>
        /// 线程
        /// </summary>
        private Task runTask;
        
        /// <summary>
        ///     取消
        /// </summary>
        private CancellationTokenSource _cancelToken;

        /// <summary>
        ///     运行参数
        /// </summary>
        public string Param { get; private set; }

        internal string AgentClass { get; set; }

        /// <summary>
        ///     唯一标示
        /// </summary>
        internal string Guid { get; set; }
        
        /// <summary>
        /// 记录run这个命令执行的时候的jobid
        /// </summary>
        internal string RunActionJobId { get; set; }

        /// <summary>
        ///     开始时间
        /// </summary>
        internal DateTime? StartTime { get; set; }

        /// <summary>
        ///     最后接
        /// </summary>
        internal DateTime? LastEndTime { get; set; }


        internal JobStatus JobStatus
        {
            get => jobStatus;
            set
            {
                jobStatus = value;
                if (value != JobStatus.Stoped)
                    return;
                LastEndTime = DateTime.Now;
            }
        }

        /// <summary>
        ///     多例job结束事件
        /// </summary>
        internal event EventHandler<TransitentJobDisposeArgs> TransitentJobDisposeEvent;

        public abstract Task OnStart(JobContext jobContext);

        /// <summary>
        /// 接收hangfire调度的stop指令
        /// </summary>
        /// <param name="jobContext"></param>
        public virtual Task OnStop(JobContext jobContext)
        {
            lock (this)
            {
                this._cancelToken?.Cancel();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// job实例如果不实现的话 就会走默认的 
        /// </summary>
        /// <param name="jobContext"></param>
        /// <param name="ex"></param>
        public virtual void OnException(JobContext jobContext, Exception ex)
        {
            //这个变成虚方法了 如果job不实现的话 设置.
            ReportToHangfireServer(jobContext, ex);
        }

        /// <summary>
        /// 上报 不管成功还是错误 一个job只会上报一次
        /// </summary>
        private void ReportToHangfireServer(JobContext jobContext, Exception ex)
        {
            try
            {
                var excuteTime = jobContext.GetElapsedMilliseconds();
                if (excuteTime < 0) return;
                var key = "_agent_result_";
                var value = Newtonsoft.Json.JsonConvert.SerializeObject(new {Id = jobContext.JobItem.JobId,Action=jobContext.ActionType,RunId=jobContext.RunJobId,R = ex!=null?"err":"ok",E =ex==null?"": ex.ToString()});
                
                jobContext.HangfireStorage?.SetRangeInHash(key+jobContext.JobItem.JobId,new List<KeyValuePair<string, string>>
                {
                    //拿到实际的执行时间
                    new KeyValuePair<string, string>(excuteTime+"",value)
                });
                
                jobContext.HangfireStorage?.AddToSet(key,jobContext.JobItem.JobId,1);
                
            }
            catch (Exception)
            {
                //ignore
            }
        }


        internal void Run(JobItem jobItem, IHangfireConsole console, IHangfireStorage storage,ConcurrentDictionary<string, string> headers)
        {
            var jobContext = new JobContext()
            {
                Param = jobItem.JobParam,
                JobItem = jobItem,
                Console = console,
                Headers = headers,
                HangfireStorage = storage,
                RunJobId = jobItem.JobId,
                HangfireServerId = jobItem.HangfireServerId,
                ActionType = "run"
            };

            if (JobStatus == JobStatus.Running)
            {
                ReportToHangfireServer(jobContext, null);
                return;
            }

            try
            {
                while (Interlocked.CompareExchange(ref locked, 1, 0) != 0)
                {
                    continue; // spin
                }
                
                if (JobStatus == JobStatus.Running)  
                {
                    ReportToHangfireServer(jobContext, null);
                    return;
                }
                
                this. _mainThread = new ManualResetEvent(false);
                this._cancelToken = new CancellationTokenSource();
                jobContext.CancelToken = this._cancelToken;
                this.Param = jobItem.JobParam;
                this.RunActionJobId = jobItem.JobId;
                
                if (this.Hang)
                {
                    runTask = Task.Factory.StartNew(async () => {
                        await start(jobContext);
                    }, TaskCreationOptions.LongRunning).Unwrap();
                }
                else
                {
                    runTask = Task.Factory.StartNew(async () => {
                        await start(jobContext);
                    }).Unwrap();
                }
                
                runTask.ContinueWith(
                    _ => { ReportToHangfireServer(jobContext,new TaskSchedulerException("runTask fail:"+_?.Exception?.Message)); }, 
                    TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                locked = 0;
            }
        }

        internal void Stop(JobItem jobItem, IHangfireConsole console,IHangfireStorage storage, ConcurrentDictionary<string, string> headers)
        {
            JobContext jobContext = new JobContext(_cancelToken)
            {
                Param = Param,
                JobItem = jobItem,
                Console = console,
                Headers = headers,
                HangfireStorage = storage,
                RunJobId = this.RunActionJobId,
                HangfireServerId = jobItem.HangfireServerId,
                ActionType = "stop"
            };
            
            if (JobStatus == JobStatus.Stoped || JobStatus == JobStatus.Stopping)
            {
                ReportToHangfireServer(jobContext, null);
                return;
            }

            try
            {
                while (Interlocked.CompareExchange(ref locked, 1, 0) != 0)
                {
                    continue; // spin
                }
                
                if (JobStatus == JobStatus.Stoped || JobStatus == JobStatus.Stopping)
                {
                    ReportToHangfireServer(jobContext, null);
                    return;
                }
                JobStatus = JobStatus.Stopping;
                if (Hang)
                {
                    _mainThread?.Set();
                }
                
                Task.Factory.StartNew(async () => {
                        await stop(jobContext);
                    })
                    .Unwrap()
                    .ContinueWith(
                        _ =>
                        {
                            ReportToHangfireServer(jobContext,new TaskSchedulerException("runTask fail:"+_?.Exception?.Message));
                        }, 
                        TaskContinuationOptions.OnlyOnFaulted);
            }
            finally
            {
                locked = 0;
            }
        }

        private async Task stop(JobContext jobContext)
        {
            try
            {
                JobStatus = JobStatus.Stopping;
                if (Hang)
                {
                    WriteToDashBordConsole(jobContext.Console, $"【Job Hang OnStop】{AgentClass}");
                }
                else
                {
                    WriteToDashBordConsole(jobContext.Console,
                        $"【{(Singleton ? "SingletonJob" : "TransientJob")} OnStop】{AgentClass}");
                }
                await OnStop(jobContext);
            }
            catch (Exception e)
            {
                WriteToDashBordConsole(jobContext.Console, Hang
                    ? $"【HangJob OnStop With Error】{AgentClass}，ex：{e.Message}"
                    : $"【{(Singleton ? "SingletonJob" : "TransientJob")} OnStop With Error】{AgentClass}，ex：{e.Message}");


                e.Data.Add("Method", "OnStop");
                e.Data.Add("AgentClass", AgentClass);
                try
                {
                    OnException(jobContext, e);
                }
                catch (Exception ex2)
                {
                    //自己overide OnException了 但是里面又抛出异常了
                    ReportToHangfireServer(jobContext, ex2);
                }
            }
            finally
            {
                JobStatus = JobStatus.Stoped;
                ReportToHangfireServer(jobContext, null);
                DisposeJob(jobContext);
            }
        }

        /// <summary>
        ///     job运行 在一个独立的线程中运行
        /// </summary>
        private async Task start(JobContext jobContext)
        {
            try
            {
                StartTime = DateTime.Now;
                JobStatus = JobStatus.Running;
                WriteToDashBordConsole(jobContext.Console, Hang
                    ? $"【HangJob OnStart】{AgentClass}"
                    : $"【{(Singleton ? "SingletonJob" : "TransientJob")} OnStart】{AgentClass}");
                await OnStart(jobContext);
                if (Hang)
                {
                    WriteToDashBordConsole(jobContext.Console, $"【Job Hang Success】{AgentClass}");
                    _mainThread.WaitOne();
                    _mainThread = null;
                }
            }
            catch (Exception e)
            {
                WriteToDashBordConsole(jobContext.Console, Hang
                    ? $"【HangJob OnStart With Error】{AgentClass}，ex：{e.Message}"
                    : $"【{(Singleton ? "SingletonJob" : "TransientJob")} OnStart With Error】{AgentClass}，ex：{e.Message}");


                e.Data.Add("Method", "OnStart");
                e.Data.Add("AgentClass", AgentClass);
                try
                {
                    OnException(jobContext, e);
                }
                catch (Exception ex2)
                {
                    //自己overide OnException了 但是里面又抛出异常了
                    ReportToHangfireServer(jobContext, ex2);
                }
            }
            finally
            {
                JobStatus = JobStatus.Stoped;
                ReportToHangfireServer(jobContext, null);
                DisposeJob(jobContext);
            }
        }

        private void WriteToDashBordConsole(IHangfireConsole console, string message)
        {
            try
            {
                console.WriteLine(message, ConsoleFontColor.DarkGreen);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        /// <summary>
        ///     获取job的详情
        /// </summary>
        /// <returns></returns>
        internal string GetJobInfo()
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(AgentClass))
            {
                list.Add($"JobClass:【{AgentClass}】");
                list.Add($"JobType:【{(Singleton ? "Singleton" : "Transient")}】");
                list.Add($"JobStatus:【{JobStatus.ToString()}】");
                list.Add($"ExcuteParam:【{Param ?? string.Empty}】");
            }

            list.Add($"StartTime:【{(StartTime == null ? "not start yet!" : StartTime.Value.ToString("yyyy-MM-dd HH:mm:ss"))}】");
            list.Add($"LastEndTime:【{(LastEndTime == null ? "not end yet!" : LastEndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"))}】");
            if (JobStatus == JobStatus.Running && StartTime != null)
            {
                list.Add($"RunningTime:【{CodingUtil.ParseTimeSeconds((int)(DateTime.Now - StartTime.Value).TotalSeconds)}】");
                if (this.runTask != null)
                {
                    try
                    {
                        list.Add($"ThreadState:【{runTask.Status.ToString()}】");
                    }
                    catch (Exception)
                    {
                        //ignore Dispose
                    }

                }
            }
            return string.Join("\r\n", list);
        }

        private void DisposeJob(JobContext jobContext)
        {
            if (jobContext.Console != null)
            {
                WriteToDashBordConsole(jobContext.Console, Hang
                    ? $"【HangJob End】{AgentClass}"
                    : $"【{(Singleton ? "SingletonJob" : "TransientJob")} End】{AgentClass}");
            }


            try
            {
                if (!Singleton) TransitentJobDisposeEvent?.Invoke(null, new TransitentJobDisposeArgs(AgentClass, Guid));
            }
            catch (Exception)
            {
                //ignore
            }

        }

        
    }
}
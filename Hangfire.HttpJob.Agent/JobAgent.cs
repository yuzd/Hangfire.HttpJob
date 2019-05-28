using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Agent
{
    public abstract class JobAgent
    {
        /// <summary>
        /// 线程
        /// </summary>
        private Thread thd;

        private  ManualResetEvent _mainThread ;

        private JobStatus jobStatus;


        /// <summary>
        /// 运行参数
        /// </summary>
        public string Param { get; private set; }
        internal string AgentClass { get; set; }

        internal volatile bool Singleton = false;

        internal volatile bool Hang = false;

        /// <summary>
        /// 开始时间
        /// </summary>
        internal DateTime StartTime { get; set; }

        /// <summary>
        /// 最后接
        /// </summary>
        internal DateTime LastEndTime { get; set; }


        internal JobStatus JobStatus
        {
            get => this.jobStatus;
            set
            {
                this.jobStatus = value;
                if (value != JobStatus.Stoped)
                    return;
                this.LastEndTime = DateTime.Now;
            }
        }

        protected abstract Task OnStart(string param);
        protected abstract void OnStop();
        protected abstract void OnException(Exception ex);


        internal void Run(string param)
        {
            if (this.JobStatus == JobStatus.Running) return;
            lock (this)
            {
                if (this.JobStatus == JobStatus.Running) return;
                _mainThread = new ManualResetEvent(false);
                this.Param = param;
                this.thd = new Thread(async () => { await this.start(); });
                this.thd.Start();
            }
        }

        internal void Stop()
        {
            if (this.JobStatus == JobStatus.Stoped || this.JobStatus == JobStatus.Stopping)
                return;

            lock (this)
            {
                try
                {
                    if (this.JobStatus == JobStatus.Stoped || this.JobStatus == JobStatus.Stopping)
                        return;

                    if (Hang)
                    {
                        _mainThread.Set();
                    }

                    this.JobStatus = JobStatus.Stopping;
                    this.OnStop();
                    this.JobStatus = JobStatus.Stoped;
                }
                catch (Exception e)
                {
                    e.Data.Add("Method", "OnStop");
                    e.Data.Add("AgentClass", AgentClass);
                    try
                    {
                        OnException(e);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }
            
        }

        /// <summary>
        /// job运行 在一个独立的线程中运行
        /// </summary>
        private async Task start()
        {
            try
            {
                if (this.JobStatus == JobStatus.Running) return;
                this.LastEndTime = DateTime.MinValue;
                this.StartTime = DateTime.Now;
                this.JobStatus = JobStatus.Running;
                await this.OnStart(this.Param);
                if (Hang)
                {
                    _mainThread.WaitOne();
                }
                this.JobStatus = JobStatus.Stoped;
            }
            catch (Exception e)
            {
                e.Data.Add("Method", "OnStart");
                e.Data.Add("AgentClass", AgentClass);
                try
                {
                    OnException(e);
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        internal string GetJobInfo()
        {
            return this.GetType().FullName;
        }
    }
}

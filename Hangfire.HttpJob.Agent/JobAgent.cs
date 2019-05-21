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


        private JobStatus jobStatus;

        /// <summary>
        /// 运行参数
        /// </summary>
        public string Param { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        internal DateTime StartTime { get; set; }

        /// <summary>
        /// 最后接
        /// </summary>
        internal DateTime lastEndTime { get; set; }


        public JobStatus JobStatus
        {
            get => this.jobStatus;
            set
            {
                this.jobStatus = value;
                if (value != JobStatus.Stoped)
                    return;
                this.lastEndTime = DateTime.Now;
            }
        }

        protected abstract Task OnStartAsync(string param);
        protected abstract Task OnStopAsync();


        internal void Run(string param)
        {
            this.Param = param;
            this.thd = new Thread(async ()=> await this.start());
            this.thd.Start();
        }

        internal async Task StopAsync()
        {
            try
            {
                this.JobStatus = JobStatus.Stopping;
                if (this.JobStatus == JobStatus.Stoped)
                    return;
                await this.OnStopAsync();
                this.JobStatus = JobStatus.Stoped;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// job运行 在一个独立的线程中运行
        /// </summary>
        private async Task start()
        {
            try
            {
                this.lastEndTime = DateTime.MinValue;
                this.JobStatus = JobStatus.Running;
                await this.OnStartAsync(this.Param);
                this.JobStatus = JobStatus.Stoped;
            }
            catch (Exception e)
            {
            }
        }
    }
}

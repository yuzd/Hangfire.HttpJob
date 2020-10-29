using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.HttpJob.Server;

namespace Hangfire.HttpJob.Support
{
    class HttpJobDisplayNameAttribute: JobDisplayNameAttribute
    {
        
        public override string Format(DashboardContext context, Job job)
        {
            var data = job.Args.FirstOrDefault() as HttpJobItem;
            if (data == null) return job.Method.Name;
            try
            {

                if (!string.IsNullOrEmpty(data.AgentClass))
                {
                    return "Agent:"+data.AgentClass+ ",Queue:" + data.QueueName + ",Retry:" + (data.EnableRetry) + "|" + data.AgentClass.Split(',')[0].Split('.').Last();
                }

                return data.Url.Replace("|","").Replace("\"","“").Replace("'","’") + ",Queue:" + data.QueueName + ",Retry:" + (data.EnableRetry) + "|" + (data.Url.Split('/').LastOrDefault() ?? data.JobName);
            }
            catch (Exception)
            {
                return data.JobName;
            }
        }

        public HttpJobDisplayNameAttribute(string displayName) : base(displayName)
        {
        }
    }
}

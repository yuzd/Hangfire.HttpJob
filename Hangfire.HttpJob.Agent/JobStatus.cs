using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Agent
{
    internal enum JobStatus
    {
        Default,
        Running,
        Stopping,
        Stoped,
    }
}

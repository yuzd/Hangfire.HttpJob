using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Client
{
    public class HangfireAddJobResult
    {
        public string ErrMessage { get; set; }
        public bool IsSuccess { get; set; }
    }
}

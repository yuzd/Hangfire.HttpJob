using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.HttpJob.Client
{
    public class HangfirJobResult
    {
        public string ErrMessage { get; set; }
        public bool IsSuccess { get; set; }

    }

    public class AddBackgroundHangfirJobResult: HangfirJobResult
    {
        public string JobId { get; set; }
    }
}

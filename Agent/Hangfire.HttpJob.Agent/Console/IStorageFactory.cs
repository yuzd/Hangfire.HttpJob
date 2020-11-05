using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent
{
    public interface IStorageFactory
    {
        IHangfireStorage CreateHangfireStorage(JobStorageConfig config);
        IHangfireConsole CreateHangforeConsole(IHangfireStorage storage);
    }
}

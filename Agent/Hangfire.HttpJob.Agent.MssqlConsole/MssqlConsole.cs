using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlConsole : HangfireConsole
    {
        private readonly IHangfireStorage _storage;

        public MssqlConsole(IHangfireStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(IHangfireStorage));
            _storage = storage;
        }

        public override IHangfireStorage Storage => _storage;
    }

}

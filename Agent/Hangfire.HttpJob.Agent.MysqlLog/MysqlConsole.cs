using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    internal class MysqlConsole : HangfireConsole
    {
        private readonly IHangfireStorage _storage;

        public MysqlConsole(IHangfireStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(IHangfireStorage));
            _storage = storage;
        }

        public override IHangfireStorage Storage => _storage;
    }

 
}

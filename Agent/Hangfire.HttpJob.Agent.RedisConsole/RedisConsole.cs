using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    internal class RedisConsole : HangfireConsole
    {
        private readonly IHangfireStorage _storage;

        public RedisConsole(IHangfireStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(IHangfireStorage));
            _storage = storage;
        }

        public override IHangfireStorage Storage => Storage;
    }

}

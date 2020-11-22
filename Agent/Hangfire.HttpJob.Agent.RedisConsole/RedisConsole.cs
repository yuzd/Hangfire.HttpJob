using System;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    internal class RedisConsole : HangfireConsole
    {
        public RedisConsole(IHangfireStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(IHangfireStorage));
        }

        public override IHangfireStorage Storage { get; }
    }

}

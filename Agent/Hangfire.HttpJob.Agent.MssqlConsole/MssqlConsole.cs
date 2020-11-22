using System;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlConsole : HangfireConsole
    {
        public MssqlConsole(IHangfireStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(IHangfireStorage));
        }

        public override IHangfireStorage Storage { get; }
    }

}

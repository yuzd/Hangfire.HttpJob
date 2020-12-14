using System;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole
{
    internal class PostgreSqlConsole : HangfireConsole
    {
        public PostgreSqlConsole(IHangfireStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(IHangfireStorage));
        }

        public override IHangfireStorage Storage { get; }
    }
}
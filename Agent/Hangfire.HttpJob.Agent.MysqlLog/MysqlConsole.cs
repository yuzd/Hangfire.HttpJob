using System;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{
    internal class MysqlConsole : HangfireConsole
    {
        public MysqlConsole(IHangfireStorage storage)
        {
            Storage = storage ?? throw new ArgumentNullException(nameof(IHangfireStorage));
        }

        public override IHangfireStorage Storage { get; }
    }

 
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Attribute
{
    /// <summary>
    /// 支持OnStart 运行支持 Hode住
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HangJobUntilStopAttribute : JobAttribute
    {
        public bool On { get; set; } = true;

        public HangJobUntilStopAttribute()
        {
        }
        public HangJobUntilStopAttribute(string registerId)
        {
            this.RegisterId = registerId;
        }
        public HangJobUntilStopAttribute(bool on)
        {
            On = on;
        }
        
        public HangJobUntilStopAttribute(string registerId,bool on)
        {
            this.RegisterId = registerId;
            On = on;
        }
    }
}

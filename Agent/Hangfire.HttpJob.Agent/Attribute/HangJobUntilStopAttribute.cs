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
        public HangJobUntilStopAttribute(string registerName)
        {
            this.RegisterName = registerName;
        }
        public HangJobUntilStopAttribute(bool on)
        {
            On = on;
        }
        
        public HangJobUntilStopAttribute(string registerName,bool on)
        {
            this.RegisterName = registerName;
            On = on;
        }
    }
}

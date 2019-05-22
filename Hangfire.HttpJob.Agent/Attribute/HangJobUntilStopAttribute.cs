using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Attribute
{
    /// <summary>
    /// 支持OnStart 运行支持 Hode住
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HangJobUntilStopAttribute : System.Attribute
    {
        public bool On { get; set; }
        public HangJobUntilStopAttribute(bool on)
        {
            On = on;
        }
    }
}

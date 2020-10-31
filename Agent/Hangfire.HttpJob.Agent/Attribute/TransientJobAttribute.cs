using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Attribute
{
    /// <summary>
    /// 支持并发运行
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TransientJobAttribute : System.Attribute
    {

    }
}

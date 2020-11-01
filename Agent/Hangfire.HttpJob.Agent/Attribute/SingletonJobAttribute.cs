using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Attribute
{
    /// <summary>
    /// 单例的 如果没运行玩再次运行会忽略执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SingletonJobAttribute : JobAttribute
    {
        public SingletonJobAttribute()
        {
            
        }
        public SingletonJobAttribute(string registerName)
        {
            this.RegisterName = registerName;
        }
    }
}

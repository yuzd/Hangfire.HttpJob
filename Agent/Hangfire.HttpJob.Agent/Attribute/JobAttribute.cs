using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Hangfire.HttpJob.Agent.Attribute
{
    /// <summary>
    /// 表达式
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class JobAttribute: System.Attribute
    {
        internal bool? enableAutoRegister;
        
        /// <summary>
        /// 是否开启自动注册
        /// </summary>
        public bool EnableAutoRegister {
            get
            {
                if (enableAutoRegister == null) return false;
                return enableAutoRegister.Value;
            }
            set { enableAutoRegister = value; }
        }

        /// <summary>
        /// 注册的job名称 为空的话默认是 job的class 和namespace的 md5 16位的
        /// </summary>
        public string RegisterName { get; set; }
   
    }
}
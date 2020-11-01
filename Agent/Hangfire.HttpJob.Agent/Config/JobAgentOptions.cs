using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Config
{
    /// <summary>
    /// JobAgent
    /// </summary>
    public class JobAgentOptions
    {
        /// <summary>
        /// 是否开启jobagent功能 总开关
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// jobagent的路由
        /// </summary>
        public string SitemapUrl { get; set; } = "/jobagent";

        /// <summary>
        /// 是否开启basicAuth验证
        /// </summary>
        public bool EnabledBasicAuth { get; set; }
        
        /// <summary>
        /// basicauth验证用户名
        /// </summary>
        public string BasicUserName { get; set; } 
        
        /// <summary>
        /// basicauth验证密码
        /// </summary>
        public string BasicUserPwd { get; set; } 
        
        /// <summary>
        /// 是否开启自动注册的开关
        /// </summary>
        public bool EnableAutoRegister { get; set; }
        
        /// <summary>
        /// 注册job的地址 也就是hangfire调度server的地址
        /// </summary>
        public string RegisterHangfireUrl { get; set; }
        /// <summary>
        /// 当前启动agent的Host
        /// </summary>
        public string RegisterAgentHost { get; set; }
        
        /// <summary>
        /// basicauth验证用户名
        /// </summary>
        public string RegisterHangfireBasicName { get; set; } 
        
        /// <summary>
        /// basicauth验证密码
        /// </summary>
        public string RegisterHangfireBasicPwd { get; set; } 
    }
}

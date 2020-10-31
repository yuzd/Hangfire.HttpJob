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
        public bool Enabled { get; set; } = true;
        public string SitemapUrl { get; set; } = "/jobagent";

        public bool EnabledBasicAuth { get; set; }
        public string BasicUserName { get; set; } 
        public string BasicUserPwd { get; set; } 
    }
}

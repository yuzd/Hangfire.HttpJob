using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Config
{
    public class JobAgentOptions
    {
        public bool Enabled { get; set; } = true;
        public string SitemapUrl { get; set; } = "/jobagent";
    }
}

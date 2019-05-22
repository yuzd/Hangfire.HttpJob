using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.HttpJob.Agent.Config
{
    public sealed class JobAgentOptionsConfigurer
    {
        private readonly JobAgentOptions options;

        internal JobAgentOptionsConfigurer(JobAgentOptions options)
        {
            this.options = options;
        }

        public JobAgentOptionsConfigurer Enabled(bool enable = true)
        {
            options.Enabled = enable;
            return this;
        }

        public JobAgentOptionsConfigurer WithSitemap(string absoluteSitemapUri)
        {
            options.SitemapUrl = absoluteSitemapUri;
            return this;
        }
    }
}

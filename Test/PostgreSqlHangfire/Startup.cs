using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PostgreSqlHangfire
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            JsonConfig = configuration;
        }

        public IConfiguration JsonConfig { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSelfHangfire(JsonConfig);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory logging)
        {
            app.ConfigureSelfHangfire(JsonConfig);
        }
    }
}

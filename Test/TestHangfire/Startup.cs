using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.HttpJob;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.MySql;
using Hangfire.Tags;
using Hangfire.Tags.MySql;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace MysqlHangfire
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
using System;
using System.Globalization;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace Hangfire.HttpJob.Demo
{
    public class Startup : IStartup
    {
        public Startup(IConfiguration configuration)
        {
            
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddSelfHangfire(Configuration);
            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            var services = app.ApplicationServices;
            var env = services.GetService<IHostingEnvironment>();
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            #region NLOG

            LogManager.LoadConfiguration("NLog.Config");

            services.GetService<ILoggerFactory>().AddNLog();

            #endregion

            //强制显示中文
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");

            //强制显示英文
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("");


            app.ConfigureHangfire();


            app.Run(async context => { await context.Response.WriteAsync("Hello World!"); });
        }

    }
}
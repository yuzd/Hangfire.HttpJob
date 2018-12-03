using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.BasicAuthorization;
using Hangfire.MySql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Hangfire.HttpJob;

namespace TestConsole
{
    public class Startup
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(Configuration);
        }

        private void Configuration(IGlobalConfiguration globalConfiguration)
        {
            globalConfiguration.UseStorage(
                    new MySqlStorage(
                        "Server=localhost;Port=3306;Database=hangfire;Uid=root;Pwd=123456;charset=utf8;SslMode=none;Allow User Variables=True",
                        new MySqlStorageOptions
                        {
                            TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                            QueuePollInterval = TimeSpan.FromSeconds(15),
                            JobExpirationCheckInterval = TimeSpan.FromHours(1),
                            CountersAggregateInterval = TimeSpan.FromMinutes(5),
                            PrepareSchemaIfNecessary = false,
                            DashboardJobListLimit = 50000,
                            TransactionTimeout = TimeSpan.FromMinutes(1),
                        }))
                .UseConsole()
                .UseHangfireHttpJob();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHangfireServer();
            app.UseHangfireDashboard("/hangfire",new DashboardOptions
            {
                Authorization = new[] { new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
                {
                    RequireSsl = false,
                    SslRedirect = false,
                    LoginCaseSensitive = true,
                    Users = new []
                    {
                        new BasicAuthAuthorizationUser
                        {
                            Login = "admin",
                            PasswordClear =  "test"
                        } 
                    }

                }) }
            });
        }
    }
}

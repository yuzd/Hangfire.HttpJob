using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestHangfirePostgreSqlAgent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
#if DEBUG
                            logging.AddConsole();
                            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);

#else
                             logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
#endif
                        }).UseUrls("http://*:5002");
                });


    }
}
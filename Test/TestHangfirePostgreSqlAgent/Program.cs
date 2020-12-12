using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace TestHangfirePostgreSqlAgent {
    public class Program {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder ()
            .SetBasePath (Directory.GetCurrentDirectory ())
            .AddJsonFile ("appsettings.json", optional : true, reloadOnChange : true)
            // .AddJsonFile ($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional : true)
            .AddEnvironmentVariables ()
            .Build ();
        public static void Main (string[] args) {
            try {
                Console.Out.WriteLine ($"当前环境{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
                PrepareLog ();
                Log.Information ("Starting auto-iv jobapi host");
                CreateWebHostBuilder (args).Build ().Run ();
            } catch (Exception ex) {
                Log.Fatal (ex, "Host terminated unexpectedly");
            } finally {
                Log.CloseAndFlush ();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder (string[] args) =>
            WebHost.CreateDefaultBuilder (args)
            .UseConfiguration (Configuration)
            .UseKestrel ()
            .UseStartup<Startup> ()
            .UseSerilog ();

        private static void PrepareLog () {
            Log.Logger = new LoggerConfiguration ()
                .ReadFrom.Configuration (Configuration)
                .CreateLogger ();
        }
    }
}
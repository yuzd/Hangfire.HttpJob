using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var build = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

            build.Run();
        }
    }
}

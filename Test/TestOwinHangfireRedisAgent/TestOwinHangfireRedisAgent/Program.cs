using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;

[assembly: OwinStartup(typeof(TestOwinHangfireRedisAgent.Startup))]
namespace TestOwinHangfireRedisAgent
{
    class Program
    {
        const string url = "http://localhost:5366";
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Server started at:" + url);
                Console.ReadLine();
            }
        }
    }
}

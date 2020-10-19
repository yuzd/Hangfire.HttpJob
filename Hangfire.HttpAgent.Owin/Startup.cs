//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Web.Mvc;
//using Hangfire.HttpAgent.Owin.Config;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Microsoft.Owin;
//using Owin;

//[assembly: OwinStartup(typeof(Hangfire.HttpAgent.Owin.Startup))]

//namespace Hangfire.HttpAgent.Owin
//{
//    public class Startup
//    {
//        public void Configuration(IAppBuilder app)
//        {
//            var services = new ServiceCollection();
//            ConfigureServices(services);

//            services.AddLogging();

//            var resolver = new DefaultDependencyResolver(services.BuildServiceProvider());
//            DependencyResolver.SetResolver(resolver);


//        }

//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddHangfireHttpJobAgent();
//        }

        

//    }

//    public class DefaultDependencyResolver : IDependencyResolver
//    {
//        protected IServiceProvider serviceProvider;

//        public DefaultDependencyResolver(IServiceProvider serviceProvider)
//        {
//            this.serviceProvider = serviceProvider;
//        }

//        public object GetService(Type serviceType)
//        {
//            return this.serviceProvider.GetService(serviceType);
//        }

//        public IEnumerable<object> GetServices(Type serviceType)
//        {
//            return this.serviceProvider.GetServices(serviceType);
//        }

//        IEnumerable<object> IDependencyResolver.GetServices(Type serviceType)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

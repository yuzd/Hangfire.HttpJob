using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Hangfire.HttpJob.Agent.Attribute;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.HttpJob.Agent.Config
{


    public class JobAgentServiceConfigurer
    {

        internal static readonly ConcurrentDictionary<Type, JobMetaData> JobAgentDic = new ConcurrentDictionary<Type, JobMetaData>();

        public IServiceCollection Services { get; }

        public JobAgentServiceConfigurer(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;
        }

        /// <summary>
        /// 添加一次 只能运行一次 运行结束才能运行下一次
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public JobAgentServiceConfigurer AddJobAgent<T>() where T : JobAgent
        {
            var type = typeof(T);
            AddJobAgent(type);
            return this;
        }

        public JobAgentServiceConfigurer AddJobAgent(Type type)
        {
            if (!typeof(JobAgent).IsAssignableFrom(type))
            {
                throw new InvalidCastException($"type:{type.Name} is not AssignableFrom typeOf JobAgent");
            }

            if (JobAgentDic.ContainsKey(type))
            {
                throw new InvalidOperationException($"type:{type.Name} is registerd!");
            }

            //这三种标签不可以共存
            var scopedJobAttribute = type.GetCustomAttribute<TransientJobAttribute>();
            var singletonJobAttribute = type.GetCustomAttribute<SingletonJobAttribute>();//没有它就是默认的
            var hangJobUntilStopAttribute = type.GetCustomAttribute<HangJobUntilStopAttribute>();
            if(scopedJobAttribute==null && hangJobUntilStopAttribute == null && singletonJobAttribute == null) singletonJobAttribute = new SingletonJobAttribute();
            var array = new object[] {scopedJobAttribute, singletonJobAttribute, hangJobUntilStopAttribute};
           
            if (array.Count(r => r != null) > 1)
            {
                throw new InvalidCastException($"type:{type.Name} can not init with mulit xxxxJobAttribute!");
            }

            if (!(array.FirstOrDefault(r=>r!=null) is JobAttribute regesterMeta))
            {
                throw new InvalidCastException($"type:{type.Name} is not AssignableFrom typeOf JobAttribute");
            }
            
            var meta = new JobMetaData
            {
                RegisterName = regesterMeta.RegisterName,
                EnableAutoRegister = regesterMeta.enableAutoRegister
            };
            
            if (hangJobUntilStopAttribute != null)
            {
                meta.Hang = hangJobUntilStopAttribute.On;
            }

            if (scopedJobAttribute != null)
            {
                meta.Transien = true;
                if (JobAgentDic.TryAdd(type, meta))
                {
                    Services.AddTransient(type);
                }
                return this;
            }
            
            meta.Transien = false;
            if (JobAgentDic.TryAdd(type, meta))
            {
                Services.AddSingleton(type);
            }
            return this;
        }

        public JobAgentServiceConfigurer AddJobAgent(Assembly assembly) 
        {
            var types = assembly.GetExportedTypes();
            var agengList = (from t in types
                where typeof(JobAgent).IsAssignableFrom(t) &&
                      !t.IsAbstract &&
                      !t.IsInterface
                select t).ToList();
            agengList.ForEach(r=>AddJobAgent(r));
            return this;
        }
    }
}

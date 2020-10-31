using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Hangfire.HttpJob.Agent.Util
{
    internal class LazyConcurrentDictionary
    {
        private readonly ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string,JobAgent>>> concurrentDictionary;

       
        public LazyConcurrentDictionary()
        {
            this.concurrentDictionary = new ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string,JobAgent>>>();
        }

        public ConcurrentDictionary<string,JobAgent> GetOrAdd(string key, Func<string, ConcurrentDictionary<string,JobAgent>> valueFactory) 
        {
            var lazyResult =
                this.concurrentDictionary.GetOrAdd(key, k => new Lazy<ConcurrentDictionary<string,JobAgent>>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));

            return lazyResult.Value;
        }

        public bool TryGetValue(string key, out ConcurrentDictionary<string,JobAgent> value)
        {
            var rt = concurrentDictionary.TryGetValue(key, out Lazy<ConcurrentDictionary<string,JobAgent>> valueLazy);
            value = rt ? valueLazy.Value : new ConcurrentDictionary<string,JobAgent>();
            return rt;
        }

        public bool TryRemove(string key, out ConcurrentDictionary<string,JobAgent> value)
        {
            var rt = concurrentDictionary.TryRemove(key, out Lazy<ConcurrentDictionary<string,JobAgent>> valueLazy);
            value = rt ? valueLazy.Value : new ConcurrentDictionary<string,JobAgent>();
            return rt;
        }

        public void JobRemove(object state,TransitentJobDisposeArgs args)
        {
            try
            {
                if(args == null) return;
                if (string.IsNullOrEmpty(args.Key) || string.IsNullOrEmpty(args.Guid)) return;
                if(this.TryGetValue(args.Key ,out var dic))
                {
                    dic.TryRemove(args.Guid,out _);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
    
    internal class TransitentJobDisposeArgs : EventArgs
    {
        public string Key { get; private set; }
        public string Guid { get; private set; }

        public TransitentJobDisposeArgs(string key,string guid)
        {
            Key = key;
            Guid = guid;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Hangfire.HttpJob.Agent.Util
{
    public class LazyConcurrentDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary;

        public LazyConcurrentDictionary()
        {
            this.concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            var lazyResult =
                this.concurrentDictionary.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));

            return lazyResult.Value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var rt = concurrentDictionary.TryGetValue(key, out Lazy<TValue> valueLazy);
            value = rt ? valueLazy.Value : default(TValue);
            return rt;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            var rt = concurrentDictionary.TryRemove(key, out Lazy<TValue> valueLazy);
            value = rt ? valueLazy.Value : default(TValue);
            return rt;
        }
    }
}
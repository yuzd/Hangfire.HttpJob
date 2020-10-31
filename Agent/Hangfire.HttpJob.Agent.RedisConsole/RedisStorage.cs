using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    internal class IRedisStorageFactory : IStorageFactory
    {
        public IHangfireStorage CreateHangfireStorage(JobStorageConfig config)
        {
            return new RedisStorage(new RedisStorageOptions
            {
                ExpireAt = config.ExpireAt,
                ExpireAtDays = config.ExpireAtDays ?? 7,
                HangfireDb = config.HangfireDb,
                DataBase = config.Db??0,
                TablePrefix = config.TablePrefix
            });
        }

        public IHangfireConsole CreateHangforeConsole(IHangfireStorage storage)
        {
            return new RedisConsole(storage);
        }
    }


    internal class RedisStorage : IHangfireStorage, IDisposable
    {
        private readonly RedisStorageOptions _options;
        private readonly Lazy<IDatabase> _redis;
        private static readonly ConcurrentDictionary<string, Lazy<IDatabase>> _redisConnectionCache = new ConcurrentDictionary<string, Lazy<IDatabase>>();
        public RedisStorage(RedisStorageOptions options)
        {
            if (options == null ) throw new ArgumentNullException(nameof(RedisStorageOptions));
            var connectionString = options.HangfireDb;
            _options = options;
            if (_options.ExpireAtDays <= 0) _options.ExpireAtDays = 7;
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            var cachekey = connectionString + options.DataBase;
            if (!_redisConnectionCache.TryGetValue(cachekey, out var redis))
            {
                redis = new Lazy<IDatabase>(() =>
                {
                    var connection = ConnectionMultiplexer.Connect(connectionString);
                    return connection.GetDatabase(options.DataBase);
                });
                
                _redisConnectionCache.TryAdd(cachekey, redis);
            }

            this._redis = redis;
        }
        public RedisStorage(IOptions<RedisStorageOptions> options):this(options.Value)
        {
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");
            var redisKey = this._options.TablePrefix + key;
            _redis.Value.HashSet(redisKey, ToHashEntries(keyValuePairs));
            _redis.Value.KeyExpire(redisKey,
                _options.ExpireAt != null
                    ? DateTime.UtcNow.Add(_options.ExpireAt.Value)
                    : DateTime.UtcNow.AddDays(_options.ExpireAtDays));
        }
        public HashEntry[] ToHashEntries(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            var hashEntry = new HashEntry[keyValuePairs.Count()];
            int i = 0;
            foreach (var kvp in keyValuePairs)
            {
                hashEntry[i] = new HashEntry(kvp.Key, kvp.Value);
                i++;
            }
            return hashEntry;
        }
        public void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);

        }

        public void AddToSet(string key, string value, double score)
        {
            var redisKey = this._options.TablePrefix + key;
            _redis.Value.SortedSetAddAsync(redisKey, value, score);

            _redis.Value.KeyExpire(redisKey,
                _options.ExpireAt != null
                    ? DateTime.UtcNow.Add(_options.ExpireAt.Value)
                    : DateTime.UtcNow.AddDays(_options.ExpireAtDays));
        }
        public void Dispose()
        {
        }
    }


}

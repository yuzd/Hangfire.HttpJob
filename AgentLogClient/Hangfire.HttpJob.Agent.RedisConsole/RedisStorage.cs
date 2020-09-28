using System;
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
                ExpireAtDays = config.ExpireAtDays ?? 7,
                HangfireDb = config.HangfireDb,
                DataBase = config.Db??0
            });
        }
    }


    internal class RedisStorage : IHangfireStorage, IDisposable
    {
        private readonly RedisStorageOptions _options;
        private readonly IDatabase _redis;
        private readonly ConnectionMultiplexer connection;
        public RedisStorage(RedisStorageOptions options)
        {
            if (options == null ) throw new ArgumentNullException(nameof(RedisStorageOptions));
            var connectionString = options.HangfireDb;
            _options = options;
            if (_options.ExpireAtDays <= 0) _options.ExpireAtDays = 7;
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            connection = ConnectionMultiplexer.Connect(connectionString);
            _redis = connection.GetDatabase(options.DataBase);
        }
        public RedisStorage(IOptions<RedisStorageOptions> options):this(options.Value)
        {
           
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            _redis.HashSet(this._options.TablePrefix + key, ToHashEntries(keyValuePairs));
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
            _redis.SortedSetAddAsync(this._options.TablePrefix + key, value, score);
        }
        public void Dispose()
        {
            connection?.Dispose();
        }
    }


}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Hangfire.HttpJob.Agent.RedisConsole
{
    internal class RedisStorage : IHangfireStorage, IDisposable
    {
        private readonly RedisStorageOptions _options;
        private readonly IDatabase _redis;
        private readonly ConnectionMultiplexer connection;

        public RedisStorage(IOptions<RedisStorageOptions> options)
        {
            if (options == null || options.Value == null) throw new ArgumentNullException(nameof(RedisStorageOptions));
            var connectionString = options.Value.HangfireDb;
            _options = options.Value;
            if (_options.ExpireAtDays <= 0) _options.ExpireAtDays = 7;
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            connection = ConnectionMultiplexer.Connect(connectionString);
            _redis = connection.GetDatabase(options.Value.DataBase);
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

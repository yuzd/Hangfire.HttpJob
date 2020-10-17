using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace Hangfire.HttpJob.Agent.MysqlConsole
{

    internal class IMysqlStorageFactory : IStorageFactory
    {
        public IHangfireStorage CreateHangfireStorage(JobStorageConfig config)
        {
            return new MySqlStorage(new MySqlStorageOptions
            {
                ExpireAt = config.ExpireAt,
                ExpireAtDays = config.ExpireAtDays ?? 7,
                HangfireDb = config.HangfireDb,
                TablePrefix = config.TablePrefix
            });
        }

        public IHangfireConsole CreateHangforeConsole(IHangfireStorage storage)
        {
            return new MysqlConsole(storage);
        }
    }


    internal class MySqlStorage : IHangfireStorage, IDisposable
    {
        private readonly string _connectionString;
        private readonly MySqlStorageOptions _options;

        public MySqlStorage(MySqlStorageOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(MySqlStorageOptions));
            _connectionString = options.HangfireDb;
            _options = options;
            if (_options.ExpireAtDays <= 0) _options.ExpireAtDays = 7;
            if (_connectionString == null) throw new ArgumentNullException("connectionString");

            if (IsConnectionString(_connectionString))
            {
                if (!_connectionString.ToLower().Contains("ignorecommandtransaction"))
                {
                    if (_connectionString.Last() != ';')
                    {
                        _connectionString += ";IgnoreCommandTransaction=true;";
                    }
                    else
                    {
                        _connectionString += "IgnoreCommandTransaction=true;";
                    }
                }
            }
            else
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not find connection string with name '{0}' in application config file",
                        _connectionString));
            }
        }

        public MySqlStorage(IOptions<MySqlStorageOptions> options):this(options.Value)
        {
        }




        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }




        internal MySqlConnection CreateAndOpenConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();

            return connection;
        }
        internal T UseConnection<T>(Func<MySqlConnection, T> func)
        {
            MySqlConnection connection = null;

            try
            {
                connection = CreateAndOpenConnection();
                return func(connection);
            }
            finally
            {
                ReleaseConnection(connection);
            }
        }
        internal void ReleaseConnection(IDbConnection connection)
        {
            if (connection != null)
            {
                connection.Dispose();
            }
        }


        internal void UseTransaction(Action<MySqlConnection> action)
        {
            UseTransaction(connection =>
            {
                action(connection);
                return true;
            }, null);
        }

        internal T UseTransaction<T>(
            Func<MySqlConnection, T> func, IsolationLevel? isolationLevel)
        {
            return UseConnection(connection =>
            {
                using (MySqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    T result = func(connection);
                    transaction.Commit();

                    return result;
                }
            });
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (keyValuePairs == null) throw new ArgumentNullException("keyValuePairs");

            UseTransaction(connection =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    connection.Execute(
                        $"insert into {_options.TablePrefix}Hash (`Key`, Field, Value,ExpireAt) " +
                        "value (@key, @field, @value,@ExpireAt) " +
                        "on duplicate key update Value = @value,ExpireAt=@ExpireAt",
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value , ExpireAt = _options.ExpireAt!=null? DateTime.UtcNow.Add(_options.ExpireAt.Value) : DateTime.UtcNow.AddDays(_options.ExpireAtDays) });
                }
            });
        }

        public void AddToSet(string key, string value)
        {
            AddToSet(key, value, 0.0);
        }

        public void AddToSet(string key, string value, double score)
        {

            UseTransaction(connection =>
            {
                connection.Execute(
                    $"INSERT INTO `{_options.TablePrefix}Set` (`Key`, `Value`, `Score`,`ExpireAt`) " +
                    "VALUES (@Key, @Value, @Score,@ExpireAt) " +
                    "ON DUPLICATE KEY UPDATE `Score` = @Score",
                    new { Key = key, Value =value, Score=score, ExpireAt = _options.ExpireAt != null ? DateTime.UtcNow.Add(_options.ExpireAt.Value) : DateTime.UtcNow.AddDays(_options.ExpireAtDays) });
            });
        }
        public void Dispose()
        {
        }
    }
}

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
    internal class MySqlStorage : IConsoleStorage, IDisposable
    {
        private readonly string _connectionString;
        private readonly MySqlStorageOptions _options;

        public MySqlStorage(IOptions<MySqlStorageOptions> options)
        {
            if (options == null || options.Value == null) throw new ArgumentNullException(nameof(MySqlStorageOptions));
            _connectionString = options.Value.HangfireDb;
            _options = options.Value;
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
                        $"insert into {_options.TablePrefix}_Hash (`Key`, Field, Value,ExpireAt) " +
                        "value (@key, @field, @value,@ExpireAt) " +
                        "on duplicate key update Value = @value",
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value , ExpireAt = DateTime.Now.AddDays(_options.ExpireAtDays) });
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
                    $"INSERT INTO `{_options.TablePrefix}_Set` (`Key`, `Value`, `Score`,`ExpireAt`) " +
                    "VALUES (@Key, @Value, @Score,@ExpireAt) " +
                    "ON DUPLICATE KEY UPDATE `Score` = @Score",
                    new { Key = key, Value =value, Score=score, ExpireAt=DateTime.Now.AddDays(_options.ExpireAtDays) });
            });
        }
        public void Dispose()
        {
        }
    }
}

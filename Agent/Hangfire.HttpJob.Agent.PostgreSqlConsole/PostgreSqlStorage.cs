using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Hangfire.HttpJob.Agent.PostgreSqlConsole
{
    internal class IPostgreSqlStorageFactory : IStorageFactory
    {
        public IHangfireStorage CreateHangfireStorage(JobStorageConfig config)
        {
            return new PostgreSqlStorage(new PostgreSqlStorageOptions
            {
                ExpireAt = config.ExpireAt,
                ExpireAtDays = config.ExpireAtDays ?? 7,
                HangfireDbConnString = config.HangfireDb,
                TablePrefix = config.TablePrefix
            });
        }

        public IHangfireConsole CreateHangforeConsole(IHangfireStorage storage)
        {
            return new PostgreSqlConsole(storage);
        }
    }

    internal class PostgreSqlStorage : IHangfireStorage, IDisposable
    {
        private readonly string _connectionString;
        private readonly PostgreSqlStorageOptions _pgsqlStorageOptions;
        public PostgreSqlStorage(PostgreSqlStorageOptions pgSqlStorageOptions)
        {
            if (pgSqlStorageOptions == null) throw new ArgumentNullException(nameof(PostgreSqlStorageOptions));
            this._connectionString = pgSqlStorageOptions.HangfireDbConnString;
            this._pgsqlStorageOptions = pgSqlStorageOptions;
            if (this._pgsqlStorageOptions.ExpireAtDays <= 0) this._pgsqlStorageOptions.ExpireAtDays = 7;
            //if (this._connectionString == null) throw new ArgumentNullException("connectionString");
            if (string.IsNullOrEmpty(this._connectionString))
            {
                throw new ArgumentNullException(nameof(this._connectionString));
            }
        }

        public PostgreSqlStorage(IOptions<PostgreSqlStorageOptions> pgSqlStorageOptions)
        : this(pgSqlStorageOptions.Value) { }

        internal NpgsqlConnection CreateAndOpenConnection()
        {
            var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();

            return connection;
        }

        internal T UseConnection<T>(Func<NpgsqlConnection, T> func)
        {
            NpgsqlConnection connection = null;
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

        internal void UseTransaction(Action<NpgsqlConnection> action)
        {
            UseTransaction(connection =>
            {
                action(connection);
                return true;
            }, null);
        }

        internal T UseTransaction<T>(Func<NpgsqlConnection, T> func, IsolationLevel? isolationLevel)
        {
            return UseConnection(connection =>
            {
                using (NpgsqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted))
                {
                    T result = func(connection);
                    transaction.Commit();
                    return result;
                }
            });
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));
            UseTransaction(connection =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    var sql = $@"insert into {this._pgsqlStorageOptions.TablePrefix}hash (""key"", field, value, expireat) " +
                        "VALUES(@key, @field, @value, @expireAt) ON conflict(key, field) " +
                        "DO UPDATE SET Value = @value, expireAt = @expireAt";
                    connection.Execute(sql,
                        new
                        {
                            key = key,
                            field = keyValuePair.Key,
                            value = keyValuePair.Value,
                            expireAt = this._pgsqlStorageOptions.ExpireAt != null
                            ? DateTime.UtcNow.Add(this._pgsqlStorageOptions.ExpireAt.Value)
                            : DateTime.UtcNow.AddDays(this._pgsqlStorageOptions.ExpireAtDays)
                        });
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
                string sql = $@"INSERT INTO {this._pgsqlStorageOptions.TablePrefix}Set (""key"", value, score, expireAt) " +
                    "VALUES(@key, @value, @score, @expireAt)  ON conflict(key, value) " +
                    "DO UPDATE SET score = @score";
                connection.Execute(sql,
                    new
                    {
                        Key = key,
                        Value = value,
                        score = score,
                        expireAt = this._pgsqlStorageOptions.ExpireAt != null
                        ? DateTime.UtcNow.Add(this._pgsqlStorageOptions.ExpireAt.Value)
                        : DateTime.UtcNow.AddDays(this._pgsqlStorageOptions.ExpireAtDays)
                    });
            });
        }

        public void Dispose() { }

        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }
    }
}
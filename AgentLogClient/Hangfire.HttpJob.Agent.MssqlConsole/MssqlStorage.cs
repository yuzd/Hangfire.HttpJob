using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    public class MssqlStorage : IConsoleStorage, IDisposable
    {
        private readonly MssqlStorageOptions _options;


        public MssqlStorage(IOptions<MssqlStorageOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(MssqlStorageOptions));
            if (_options.ExpireAtDays <= 0) _options.ExpireAtDays = 7;
            if (string.IsNullOrEmpty(_options.HangfireDb))
            {
                throw new ArgumentNullException(nameof(MssqlStorageOptions.HangfireDb));
            }

            if (string.IsNullOrEmpty(_options.TablePrefix))
            {
                throw new ArgumentNullException(nameof(MssqlStorageOptions.TablePrefix));
            }
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

            UseConnection(connection =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    string sql =
                        $" insert into [{_options.TablePrefix}].Hash ([Key], Field, Value,ExpireAt) values (@key, @field, @value,@ExpireAt);";

                    connection.Execute(
                       sql,
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value, ExpireAt = DateTime.Now.AddDays(_options.ExpireAtDays) });
                }
            });


        }

        public void AddToSet(string key, string value, double score)
        {
            string addSql =
                $"insert into [{_options.TablePrefix}].[Set] ([Key], Value, Score,ExpireAt) values (@key, @value, @score,@ExpireAt);";

            UseConnection(connection =>
            {
                connection.Execute(addSql,
                    new { key = key, value= value, score =score, ExpireAt  = DateTime.Now.AddDays(_options.ExpireAtDays)});
            });

        }

        public void Dispose()
        {
        }


        internal SqlConnection CreateAndOpenConnection()
        {
            var connection = new SqlConnection(_options.HangfireDb);
            connection.Open();

            return connection;
        }

        internal void UseConnection(Action<SqlConnection> func)
        {
            SqlConnection connection = null;

            try
            {
                connection = CreateAndOpenConnection();
                func(connection);
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



    }
}

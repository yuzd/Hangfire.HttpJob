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
                        $@";merge [{_options.TablePrefix}].Hash with (holdlock) as Target
using (VALUES (@key, @field, @value)) as Source ([Key], Field, Value)
on Target.[Key] = Source.[Key] and Target.Field = Source.Field
when matched then update set Value = Source.Value
when not matched then insert ([Key], Field, Value) values (Source.[Key], Source.Field, Source.Value);";

                    connection.Execute(
                       sql,
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value });
                }
            });


        }

        public void AddToSet(string key, string value, double score)
        {
            string addSql =
                $@";merge [{_options.TablePrefix}].[Set] with (holdlock) as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

            UseConnection(connection =>
            {
                connection.Execute(addSql,
                    new { key, value, score });
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

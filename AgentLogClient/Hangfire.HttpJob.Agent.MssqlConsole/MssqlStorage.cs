using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;

namespace Hangfire.HttpJob.Agent.MssqlConsole
{
    internal class IMssqlStorageFactory : IStorageFactory
    {
        public IHangfireStorage CreateHangfireStorage(JobStorageConfig config)
        {
            return new MssqlStorage(new MssqlStorageOptions
            {
                ExpireAt = config.ExpireAt,
                ExpireAtDays = config.ExpireAtDays ?? 7,
                HangfireDb = config.HangfireDb,
                TablePrefix = config.TablePrefix
            });
        }

        public IHangfireConsole CreateHangforeConsole(IHangfireStorage storage)
        {
            return new MssqlConsole(storage);
        }
    }

    public class MssqlStorage : IHangfireStorage, IDisposable
    {
        private readonly MssqlStorageOptions _options;


        public MssqlStorage(IOptions<MssqlStorageOptions> options):this(options.Value)
        {
            
        }

        public MssqlStorage(MssqlStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(MssqlStorageOptions));
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
            var sql = $@"
set xact_abort off;
begin try
  insert into [{_options.TablePrefix}].Hash ([Key], Field, Value,ExpireAt) values (@key, @field, @value,@ExpireAt);
  if @@ROWCOUNT = 0 update [{_options.TablePrefix}].Hash set Value = @value,ExpireAt =@ExpireAt where [Key] = @key and Field = @field;
end try
begin catch
  IF ERROR_NUMBER() not in (2601, 2627) throw;
  update [{_options.TablePrefix}].Hash set Value = @value,ExpireAt =@ExpireAt where [Key] = @key and Field = @field;
end catch";
            UseConnection(connection =>
            {
                foreach (var keyValuePair in keyValuePairs)
                {
                    //string sql =
                    //    $" insert into [{_options.TablePrefix}].Hash ([Key], Field, Value,ExpireAt) values (@key, @field, @value,@ExpireAt);";

                    connection.Execute(
                       sql,
                        new { key = key, field = keyValuePair.Key, value = keyValuePair.Value, ExpireAt =_options.ExpireAt!=null ? DateTime.UtcNow.Add(_options.ExpireAt.Value) :  DateTime.UtcNow.AddDays(_options.ExpireAtDays) });
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
                    new { key = key, value= value, score =score, ExpireAt  = _options.ExpireAt != null ? DateTime.UtcNow.Add(_options.ExpireAt.Value) : DateTime.UtcNow.AddDays(_options.ExpireAtDays)});
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

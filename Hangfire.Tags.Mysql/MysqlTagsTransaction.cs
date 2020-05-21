using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Dapper;
using Hangfire.MySql.Core;
using Hangfire.Storage;
using Hangfire.Tags.Storage;
using MySql.Data.MySqlClient;

namespace Hangfire.Tags.Mysql
{
    internal class MysqlTagsTransaction : ITagsTransaction
    {
        private readonly MySqlStorageOptions _options;
        private readonly IWriteOnlyTransaction _transaction;

        private static Type _type;
        private static MethodInfo _acquireSetLock;
        private static MethodInfo _queueCommand;

        public MysqlTagsTransaction(MySqlStorageOptions options, IWriteOnlyTransaction transaction)
        {
            if (transaction.GetType().Name != "MySqlWriteOnlyTransaction")
                throw new ArgumentException("The transaction is not an SQL transaction", nameof(transaction));

            _options = options;
            _transaction = transaction;

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now

            // Other transaction type, clear cached methods
            if (_type != transaction.GetType())
            {
                _acquireSetLock = null;
                _queueCommand = null;

                _type = transaction.GetType();
            }

            if (_acquireSetLock == null)
                _acquireSetLock = transaction.GetType().GetTypeInfo().GetMethod(nameof(AcquireSetLock),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_acquireSetLock == null)
                throw new ArgumentException("The function AcquireSetLock cannot be found.");

            if (_queueCommand == null)
            {
                _queueCommand = transaction.GetType().GetTypeInfo().GetMethod(nameof(QueueCommand),
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_queueCommand == null)
                throw new ArgumentException("The functions QueueCommand  cannot be found.");

        }

        private void AcquireSetLock()
        {
            _acquireSetLock.Invoke(_transaction, null);
        }

        private void QueueCommand(Action<MySqlConnection> action)
        {
            _queueCommand.Invoke(_transaction, new[] { action });
        }

      
        public void ExpireSetValue(string key, string value, TimeSpan expireIn)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireSetLock();
            QueueCommand(r => r.Execute($@"update `{_options.TablePrefix}_Set` set ExpireAt = @expireAt where `Key` = @key and `Value` = @value",
                new
                {
                    key = key,
                    value = value,
                    expireAt = expireIn
                }));

        }

        public void PersistSetValue(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            AcquireSetLock();
            QueueCommand( r=> r.Execute($"update `{_options.TablePrefix}_Set` set ExpireAt = null where `Key` = @key and `Value` = @value",
                new
                {
                    key = key,
                    value = value
                }));
        }
    }
}

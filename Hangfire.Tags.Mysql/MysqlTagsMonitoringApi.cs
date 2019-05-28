using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Hangfire.Storage;

namespace Hangfire.Tags.Mysql
{
   internal class MysqlTagsMonitoringApi
    {
        private readonly IMonitoringApi _monitoringApi;

        private static Type _type;
        private static MethodInfo _useConnection;

        public MysqlTagsMonitoringApi(IMonitoringApi monitoringApi)
        {
      
            _monitoringApi = monitoringApi;

            // Dirty, but lazy...we would like to execute these commands in the same transaction, so we're resorting to reflection for now

            // Other transaction type, clear cached methods
            if (_type != monitoringApi.GetType())
            {
                _useConnection = null;

                _type = monitoringApi.GetType();
            }

            if (_useConnection == null)
                _useConnection = monitoringApi.GetType().GetTypeInfo().GetMethod(nameof(UseConnection),
                    BindingFlags.NonPublic | BindingFlags.Instance);

            if (_useConnection == null)
                throw new ArgumentException("The function UseConnection cannot be found.");
        }

        public T UseConnection<T>(Func<DbConnection, T> action)
        {
            var method = _useConnection.MakeGenericMethod(typeof(T));
            return (T)method.Invoke(_monitoringApi, new object[] { action });
        }
    }
}

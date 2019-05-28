using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Dapper;
using Hangfire.Common;
using Hangfire.MySql.Core;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Hangfire.Tags.Dashboard.Monitoring;
using Hangfire.Tags.Storage;

namespace Hangfire.Tags.Mysql
{
    internal class MysqlTagsServiceStorage : ITagsServiceStorage
    {
        private readonly MySqlStorageOptions _options;
        private MysqlTagsMonitoringApi MonitoringApi => new MysqlTagsMonitoringApi(JobStorage.Current.GetMonitoringApi());
        public MysqlTagsServiceStorage()
            : this(new MySqlStorageOptions())
        {
        }

        public MysqlTagsServiceStorage(MySqlStorageOptions options)
        {
            _options = options;
        }

        public ITagsTransaction GetTransaction(IWriteOnlyTransaction transaction)
        {
            return new MysqlTagsTransaction(_options, transaction);
        }

        public IEnumerable<TagDto> SearchWeightedTags(string tag = null, string setKey = "tags")
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection =>
            {

                if (string.IsNullOrEmpty(tag))
                    tag = "[^0-9]"; // Exclude tags:<id> entries

                var sql =
                    $@"select count(*) as Amount from `{_options.TablePrefix}_Set` s where s.Key REGEXP '{setKey}:{tag}'";
                var total = connection.ExecuteScalar<int>(sql);

                sql =
                    $@"select INSERT(`Key`, 1, 5, '') AS Tag, COUNT(*) AS Amount, ROUND(count(*) * 1.0 / @total * 100, 0) as Percentage
from `{_options.TablePrefix}_Set` s where s.Key REGEXP '{setKey}:{tag}' group by s.Key";

                return connection.Query<TagDto>(
                    sql,
                    new { total },
                    commandTimeout: (int?)_options.TransactionTimeout.TotalSeconds);
            });
        }

        public IEnumerable<string> SearchTags(string tag, string setKey = "tags")
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection =>
            {
                var sql =
                    $@"select Value from `{_options.TablePrefix}_Set` s where s.Key='tags' and s.Value like '@setKey:%@tag%'";

                return connection.Query<string>(
                    sql,
                    new { setKey, tag },
                    commandTimeout: (int?)_options.TransactionTimeout.TotalSeconds);
            });
        }

        public int GetJobCount(string[] tags, string stateName = null)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection => GetJobCount(connection, tags, stateName));
        }

        public IDictionary<string, int> GetJobStateCount(string[] tags, int maxTags = 50)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection =>
            {
                var parameters = new Dictionary<string, object>();

                var jobsSql =
                    $@"

  select j.Id
  from `{_options.TablePrefix}_Job` j";

                for (var i = 0; i < tags.Length; i++)
                {
                    parameters["tag" + i] = tags[i];
                    jobsSql +=
                        $"  inner join `{_options.TablePrefix}_Set` s{i} on j.Id=s{i}.Value and s{i}.Key=@tag{i}";
                }

                var sql2 =
                    $@"
select j.StateName AS `Key`, count(*) AS Value
from `{_options.TablePrefix}_Job` j
inner join ({jobsSql}) as cte on cte.Id = j.Id 
left join `{_options.TablePrefix}_State` s on j.StateId = s.Id
group by j.StateName order by count(*) desc limit {maxTags} ";

                return connection.Query<KeyValuePair<string, int>>(
                        sql2,
                        parameters,
                        commandTimeout: (int?)_options.TransactionTimeout.TotalSeconds)
                    .ToDictionary(d => d.Key, d => d.Value);
            });
        }

        public JobList<MatchingJobDto> GetMatchingJobs(string[] tags, int @from, int count, string stateName = null)
        {
            var monitoringApi = MonitoringApi;
            return monitoringApi.UseConnection(connection => GetJobs(connection, from, count, tags, stateName,
                (sqlJob, job, stateData) =>
                    new MatchingJobDto
                    {
                        Job = job,
                        State = sqlJob.StateName,
                        CreatedAt = sqlJob.CreatedAt,
                        ResultAt = GetStateDate(stateData, sqlJob.StateName)
                    }));
        }

        private JobList<TDto> GetJobs<TDto>(
            DbConnection connection, int from, int count, string[] tags, string stateName,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var parameters = new Dictionary<string, object>
            {
                { "start", from },
                { "end",  count },
                { "stateName", stateName }
            };

            var jobsSql =
                $@"
  select j.Id
  from `{_options.TablePrefix}_Job` j ";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $"  inner join `{_options.TablePrefix}_Set` s{i} on j.Id=s{i}.Value and s{i}.Key=@tag{i}";
            }

            jobsSql +=
                $@"
  where (@stateName IS NULL OR LENGTH(@stateName) = 0 OR j.StateName=@stateName)";
            var sql2 = $@"
select j.*, s.Reason as StateReason, s.Data as StateData
from `{_options.TablePrefix}_Job` j
inner join ({jobsSql}) as cte on cte.Id = j.Id 
left join `{_options.TablePrefix}_State` s on j.StateId = s.Id
order by j.Id desc
limit @start,@end 
";

            var jobs = connection.Query<SqlJob>(
                    sql2,
                    parameters,
                    commandTimeout: (int?)_options.TransactionTimeout.TotalSeconds)
                .ToList();

            return DeserializeJobs(jobs, selector);
        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = SerializationHelper.Deserialize<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.DeserializeJob();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        private static JobList<TDto> DeserializeJobs<TDto>(
            ICollection<SqlJob> jobs,
            Func<SqlJob, Job, SafeDictionary<string, string>, TDto> selector)
        {
            var result = new List<KeyValuePair<string, TDto>>(jobs.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var job in jobs)
            {
                var dto = default(TDto);

                if (job.InvocationData != null)
                {
                    var deserializedData = SerializationHelper.Deserialize<Dictionary<string, string>>(job.StateData);
                    var stateData = deserializedData != null
                        ? new SafeDictionary<string, string>(deserializedData, StringComparer.OrdinalIgnoreCase)
                        : null;

                    dto = selector(job, DeserializeJob(job.InvocationData, job.Arguments), stateData);
                }

                result.Add(new KeyValuePair<string, TDto>(job.Id.ToString(), dto));
            }

            return new JobList<TDto>(result);
        }

        private DateTime? GetStateDate(SafeDictionary<string, string> stateData, string stateName)
        {
            var stateDateName = stateName == "Processing" ? "StartedAt" : $"{stateName}At";
            return DateTime.TryParse(stateData?[stateDateName], out var result) ? result.ToUniversalTime() : (DateTime?)null;
        }

        private int GetJobCount(DbConnection connection, string[] tags, string stateName)
        {
            var parameters = new Dictionary<string, object>
            {
                {"stateName", stateName}
            };

            var jobsSql =
                $@"
  select j.Id
  from `{_options.TablePrefix}_Job` j ";

            for (var i = 0; i < tags.Length; i++)
            {
                parameters["tag" + i] = tags[i];
                jobsSql += $"  inner join `{_options.TablePrefix}_Set` s{i} on j.Id=s{i}.Value and s{i}.Key=@tag{i}";
            }

            jobsSql +=
                $@"
  where (@stateName IS NULL OR LENGTH(@stateName)=0 OR j.StateName=@stateName)";
var sql2 = $@"
select count(*)
from `{_options.TablePrefix}_Job` j 
inner join ({jobsSql}) as cte on cte.Id = j.Id 
left join `{_options.TablePrefix}_State` s  on j.StateId = s.Id";

            return connection.ExecuteScalar<int>(
                sql2,
                parameters,
                commandTimeout: (int?)_options.TransactionTimeout.TotalSeconds);
        }


        /// <summary>
        /// Overloaded dictionary that doesn't throw if given an invalid key
        /// Fixes issues such as https://github.com/HangfireIO/Hangfire/issues/871
        /// </summary>
        private class SafeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public SafeDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                : base(dictionary, comparer)
            {
            }

            public new TValue this[TKey i]
            {
                get => ContainsKey(i) ? base[i] : default(TValue);
                set => base[i] = value;
            }
        }

        internal class SqlJob
        {
            public long Id { get; set; }
            public string InvocationData { get; set; }
            public string Arguments { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpireAt { get; set; }

            public DateTime? FetchedAt { get; set; }

            public string StateName { get; set; }
            public string StateReason { get; set; }
            public string StateData { get; set; }
        }
    }


}

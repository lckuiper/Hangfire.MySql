﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.MySql.Common;
using Hangfire.MySql.src.Entities;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Job = Hangfire.Common.Job;

namespace Hangfire.MySql.src
{
    internal class MySqlMonitoringApi : ShortConnectingDatabaseActor, IMonitoringApi
    {
        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public MySqlMonitoringApi(
            string connectionString,
            PersistentJobQueueProviderCollection queueProviders)
            : base(connectionString)
        {
            _queueProviders = queueProviders;
        }

        public IList<QueueWithTopEnqueuedJobsDto> Queues()
        {
            return new List<QueueWithTopEnqueuedJobsDto>();
        }

        public IList<ServerDto> Servers()
        {
            return UsingTable<Entities.Server, IList<ServerDto>>(servers =>
            {

                var result = new List<ServerDto>();

                foreach (var server in servers)
                {
                    var data = JobHelper.FromJson<ServerData>(server.Data);

                    result.Add(new ServerDto
                    {
                        Name = server.Id,
                        Heartbeat = server.LastHeartbeat,
                        Queues = data.Queues,
                        StartedAt = data.StartedAt.HasValue ? data.StartedAt.Value : DateTime.MinValue,
                        WorkersCount = data.WorkerCount
                    });

                }

                Debug.WriteLine("MySqlMonitoringApi  Servers() returning " + result.Count);

                return result.ToList();

            });

        }

        private static Job DeserializeJob(string invocationData, string arguments)
        {
            var data = JobHelper.FromJson<InvocationData>(invocationData);
            data.Arguments = arguments;

            try
            {
                return data.Deserialize();
            }
            catch (JobLoadException)
            {
                return null;
            }
        }

        public JobDetailsDto JobDetails(string jobId)
        {
            return UsingDatabase<JobDetailsDto>(db =>
            {
                var job = db.GetTable<Entities.Job>().Single(j => j.Id == Convert.ToInt32(jobId));

                var histories = db.GetTable<Entities.JobState>().Where(js => js.JobId == job.Id).Select(jobState => new StateHistoryDto()
                {
                    CreatedAt = jobState.CreatedAt,
                    Reason = jobState.Reason,
                    StateName = jobState.Name,
                    Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(jobState.Data)
                }).ToList();


                var jobDetailsDto = new JobDetailsDto()
                {
                    CreatedAt = job.CreatedAt,
                    ExpireAt = job.ExpireAt,
                    Properties = db.GetTable<Entities.JobParameter>().Where(jp => jp.JobId == job.Id).ToDictionary(jp => jp.Name, jp => jp.Value),
                    History = histories
                };

                return jobDetailsDto;

            });

        }

        protected long GetCounterTotal(string key)
        {
            return UsingTable<Counter, long>(counters => counters.Where(c => c.Key == key).Sum(c => c.Value));
        }

        protected long GetNJobsInState(string stateName)
        {
            return UsingTable<Entities.Job, long>(jobs => jobs.Count(j => j.StateName == stateName));
        }






        public StatisticsDto GetStatistics()
        {
            return new StatisticsDto()
            {
                Deleted = GetCounterTotal("stats:deleted"),
                Enqueued = GetNJobsInState(EnqueuedState.StateName),
                Failed = GetNJobsInState(FailedState.StateName),
                Processing = GetNJobsInState(ProcessingState.StateName),
                Queues = 0,
                /* _queueProviders
                    .SelectMany(x => x.GetJobQueueMonitoringApi(connection).GetQueues())
                    .Count()
                 * */
                Recurring = UsingTable<Entities.Set, long>(sets => sets.Count(s => s.Key == "recurring-jobs")),
                Succeeded = GetCounterTotal("stats:succeeded"),
                Scheduled = GetNJobsInState(ScheduledState.StateName),
                Servers = Servers().Count
            };
        }

        public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int @from, int perPage)
        {
            return new JobList<EnqueuedJobDto>(new List<KeyValuePair<string, EnqueuedJobDto>>());
        }

        public JobList<FetchedJobDto> FetchedJobs(string queue, int @from, int perPage)
        {
            return new JobList<FetchedJobDto>(new List<KeyValuePair<string, FetchedJobDto>>());
        }

        public JobList<ProcessingJobDto> ProcessingJobs(int @from, int count)
        {
            return new JobList<ProcessingJobDto>(new List<KeyValuePair<string, ProcessingJobDto>>());
        }

        public JobList<ScheduledJobDto> ScheduledJobs(int @from, int count)
        {
            return new JobList<ScheduledJobDto>(new List<KeyValuePair<string, ScheduledJobDto>>());
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return UsingDatabase(db =>
            {

                var jobs = db.GetTable<Entities.Job>()
                    .Where(j => j.StateName == "Succeeded")
                    .OrderByDescending(j => j.Id)
                    .Skip(from)
                    .Take(count);

                var list = new List<KeyValuePair<string, SucceededJobDto>>();

                foreach (var sqlJob in jobs)
                {

                    var stateData = JsonConvert.DeserializeObject<Dictionary<string, string>>(sqlJob.StateData);

                    var s = new SucceededJobDto()
                    {
                        Job = DeserializeJob(sqlJob.InvocationData, sqlJob.Arguments),
                        InSucceededState = true,
                        Result = stateData.ContainsKey("Result") ? stateData["Result"] : null,
                        TotalDuration = stateData.ContainsKey("PerformanceDuration") && stateData.ContainsKey("Latency")
                            ? (long?)long.Parse(stateData["PerformanceDuration"]) +
                              (long?)long.Parse(stateData["Latency"])
                            : null,
                        SucceededAt = JobHelper.DeserializeNullableDateTime(stateData["SucceededAt"])
                    };

                    list.Add(new KeyValuePair<string, SucceededJobDto>(
                        sqlJob.Id.ToString(CultureInfo.InvariantCulture), s));

                }

                return new JobList<SucceededJobDto>(list);

            });

        }


        public JobList<FailedJobDto> FailedJobs(int @from, int count)
        {
            return new JobList<FailedJobDto>(new List<KeyValuePair<string, FailedJobDto>>());


        }

        public JobList<DeletedJobDto> DeletedJobs(int @from, int count)
        {
            return new JobList<DeletedJobDto>(new List<KeyValuePair<string, DeletedJobDto>>());

        }

        public long ScheduledCount()
        {
            return GetNJobsInState(ScheduledState.StateName);
        }

        public long EnqueuedCount(string queue)
        {
            //return UseConnection(connection =>
            //{
            // var queueApi = GetQueueApi(queue);
            // var counters = queueApi.GetEnqueuedAndFetchedCount(queue);

            // return counters.EnqueuedCount ?? 0;
            // });

            return 0;
        }

        public long FetchedCount(string queue)
        {
            return 0;
        }

        public long FailedCount()
        {
            return GetNJobsInState(FailedState.StateName);
        }

        public long ProcessingCount()
        {
            return GetNJobsInState(ProcessingState.StateName);
        }

        public long SucceededListCount()
        {
            return GetNJobsInState(SucceededState.StateName);
        }

        public long DeletedListCount()
        {
            return GetNJobsInState(DeletedState.StateName);
        }

        public IDictionary<DateTime, long> SucceededByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> FailedByDatesCount()
        {
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> HourlySucceededJobs()
        {
            // return new Dictionary<DateTime, long>();

            return GetHourlyTimelineStats("succeeded");
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return new Dictionary<DateTime, long>();
        }



        #region Queue API

        private Dictionary<DateTime, long> GetHourlyTimelineStats(
            string type)
        {
            var endDate = DateTime.UtcNow;
            var dates = new List<DateTime>();
            for (var i = 0; i < 24; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddHours(-1);
            }

            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd-HH")), x => x);

            return GetTimelineStats(keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(
            string type)
        {
            var endDate = DateTime.UtcNow.Date;
            var dates = new List<DateTime>();

            for (var i = 0; i < 7; i++)
            {
                dates.Add(endDate);
                endDate = endDate.AddDays(-1);
            }
            var keyMaps = dates.ToDictionary(x => String.Format("stats:{0}:{1}", type, x.ToString("yyyy-MM-dd")), x => x);

            return GetTimelineStats(keyMaps);
        }

        private Dictionary<DateTime, long> GetTimelineStats(Dictionary<string, DateTime> keyMaps)
        {
            // HAVING:
            // https://msdn.microsoft.com/en-us/library/vstudio/bb534972(v=vs.100).aspx

            var valuesMap = new Dictionary<string, long>();

            foreach (var key in keyMaps.Keys)
            {
                if (!valuesMap.ContainsKey(key)) valuesMap.Add(key, 0);
            }

            foreach (var key in keyMaps.Keys)
            {
                long counter = GetCounterTotal(key);

                if (valuesMap.ContainsKey(key))
                    valuesMap[key] += counter;
                else
                    valuesMap.Add(key, counter);
            }

            //string sqlQuery = @"
            //    SELECT ""key"", COUNT(""value"") AS ""count"" 
            //    FROM """ + _options.SchemaName + @""".""counter""
            //    GROUP BY ""key""
            //    HAVING ""key"" = ANY @keys;
            //    ";

            //var valuesMap = connection.Query(
            //    sqlQuery,
            //    new { keys = keyMaps.Keys })
            //    .ToDictionary(x => (string)x.key, x => (long)x.count);
                      

            var result = new Dictionary<DateTime, long>();
            for (var i = 0; i < keyMaps.Count; i++)
            {
                var value = valuesMap[keyMaps.ElementAt(i).Key];
                result.Add(keyMaps.ElementAt(i).Value, value);
            }

            return result;
        }

        //private IPersistentJobQueueMonitoringApi GetQueueApi(
        //   string queueName)
        //{
        //    var provider = _queueProviders.GetProvider(queueName);
        //    var monitoringApi = provider.GetJobQueueMonitoringApi(connection.ConnectionString);

        //    return monitoringApi;
        //}

        #endregion Queue API


    }
}

using System;
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
                    Properties = db.GetTable<Entities.JobParameter>().Where(jp=>jp.JobId==job.Id).ToDictionary(jp => jp.Name, jp => jp.Value),
                    History = histories
                };

                return jobDetailsDto;

            });

        }

        public StatisticsDto GetStatistics()
        {
            return new StatisticsDto();
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
                    .Where(j=>j.StateName=="Succeeded")
                    .OrderByDescending(j=>j.Id)
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
                            ? (long?) long.Parse(stateData["PerformanceDuration"]) +
                              (long?) long.Parse(stateData["Latency"])
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
            return 0;
        }

        public long EnqueuedCount(string queue)
        {
            return 0;
        }

        public long FetchedCount(string queue)
        {
            return 0;
        }

        public long FailedCount()
        {
            throw new NotImplementedException();
        }

        public long ProcessingCount()
        {
            return 0;
        }

        public long SucceededListCount()
        {
            return UsingTable<Entities.Job, long>(jobs => jobs.Count(j => j.StateName == "Succeeded"));
        }

        public long DeletedListCount()
        {
            return 0;
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
            return new Dictionary<DateTime, long>();
        }

        public IDictionary<DateTime, long> HourlyFailedJobs()
        {
            return new Dictionary<DateTime, long>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
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

namespace Hangfire.MySql.src
{
    internal class MySqlMonitoringApi : ShortConnectingDatabaseActor, IMonitoringApi
    {
        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public MySqlMonitoringApi(
            string connectionString,
            PersistentJobQueueProviderCollection queueProviders) : base(connectionString)
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

        public JobDetailsDto JobDetails(string jobId)
        {
           return new JobDetailsDto(); 

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
            throw new NotImplementedException();
        }

        public JobList<SucceededJobDto> SucceededJobs(int @from, int count)
        {
            return new JobList<SucceededJobDto>(new List<KeyValuePair<string, SucceededJobDto>>());
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
            return 0;
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

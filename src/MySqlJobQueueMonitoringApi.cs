using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.MySql.Common;
using Hangfire.MySql.src.Entities;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    public class MySqlJobQueueMonitoringApi : ShortConnectingDatabaseActor, IPersistentJobQueueMonitoringApi
    {
        private readonly MySqlStorageOptions _options;
        
        public MySqlJobQueueMonitoringApi(string connectionString, MySqlStorageOptions options)
            : base(connectionString)
        {
            options.Should().NotBeNull();
            _options = options;
        }

        public IEnumerable<string> GetQueues()
        {
            // string sqlQuery = @"SELECT DISTINCT ""queue"" FROM """ + _options.SchemaName + @""".""jobqueue"";";
            // return _connection.Query(sqlQuery).Select(x => (string)x.queue).ToList();

            return UsingDatabase(db =>
            {
                return db.GetTable<Entities.JobQueue>()
                    .Select(o => o.Queue).Distinct().ToList();
            });
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            #region SQL
            // return GetQueuedOrFetchedJobIds(queue, false, @from, perPage);
            //string sqlQuery = string.Format(@"
            //    SELECT j.""id"" 
            //    FROM """ + _options.SchemaName + @""".""jobqueue"" jq
            //    LEFT JOIN """ + _options.SchemaName + @""".""job"" j ON jq.""jobid"" = j.""id""
            //    WHERE jq.""queue"" = @queue 
            //    AND jq.""fetchedat"" {0}
            //    AND j.""id"" IS NOT NULL
            //    LIMIT @count OFFSET @start;
            //    ", fetched ? "IS NOT NULL" : "IS NULL");    
            #endregion SQL

            return UsingDatabase(db =>
            {
                var query = from jq in db.GetTable<Entities.JobQueue>()
                            join j in db.GetTable<Entities.Job>() on jq.JobId equals j.Id
                            where (
                                jq.Queue.Equals(queue) &&
                                (jq.FetchedAt == NullDateTime))
                            select j.Id;

                var results = query.Skip(@from).Take(perPage);

                return results.AsEnumerable();
            });

        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            #region SQL
            // return GetQueuedOrFetchedJobIds(queue, true, @from, perPage);
            //string sqlQuery = string.Format(@"
            //    SELECT j.""id"" 
            //    FROM """ + _options.SchemaName + @""".""jobqueue"" jq
            //    LEFT JOIN """ + _options.SchemaName + @""".""job"" j ON jq.""jobid"" = j.""id""
            //    WHERE jq.""queue"" = @queue 
            //    AND jq.""fetchedat"" {0}
            //    AND j.""id"" IS NOT NULL
            //    LIMIT @count OFFSET @start;
            //    ", fetched ? "IS NOT NULL" : "IS NULL");
            #endregion SQL

            return UsingDatabase(db =>
            {
                var query = from jq in db.GetTable<Entities.JobQueue>()
                            join j in db.GetTable<Entities.Job>() on jq.JobId equals j.Id
                            where (
                                jq.Queue.Equals(queue) &&
                                (jq.FetchedAt != null))
                            select j.Id;

                var results = query.Skip(@from).Take(perPage);

                return results.AsEnumerable();
            });
        }

        public EnqueuedAndFetchedCount GetEnqueuedAndFetchedCount(string queue)
        {
            int enqueuedCount = 0;
            int fetchedCount = 0;

            UsingDatabase(db =>
            {
                // SELECT COUNT(*) FROM """ + _options.SchemaName + @""".""jobqueue"" WHERE ""fetchedat"" IS NULL AND ""queue"" = @queue
                //                                                                                        -------
                enqueuedCount = db.GetTable<Entities.JobQueue>()
                   .Count(o => o.FetchedAt == null && o.Queue.Equals(queue));

                // SELECT COUNT(*) FROM """ + _options.SchemaName + @""".""jobqueue"" WHERE ""fetchedat"" IS NOT NULL AND ""queue"" = @queue
                //                                                                                        -----------
                fetchedCount = db.GetTable<Entities.JobQueue>()
                   .Count(o => o.FetchedAt != null && o.Queue.Equals(queue));
            });

            return new EnqueuedAndFetchedCount
            {
                EnqueuedCount = enqueuedCount,
                FetchedCount = fetchedCount
            };
        }
    }
}
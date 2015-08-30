using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.Annotations;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    internal class MySqlJobQueue : DatabaseDependant, IPersistentJobQueue
    {
        private readonly MySqlStorageOptions _options;

        public MySqlJobQueue(MySqlConnection connection, MySqlStorageOptions options) : base(connection)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (connection == null) throw new ArgumentNullException("connection");
            _options = options;
        }


        // why did Frank use his own attributes ??

        [NotNull]
        public IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken)
        {

            // TODO add queues

            string sql = "UPDATE JobQueue "
                         + " SET FetchedAt = CURTIME(), Id=LAST_INSERT_ID(Id) "
                         + " WHERE FetchedAt IS NULL "
                         + " ORDER BY Id "
                         + " LIMIT 1; "
                         + " SELECT LAST_INSERT_ID()";

            MySqlCommand comm = new MySqlCommand(sql, Connection);

            var x = Convert.ToInt32(comm.ExecuteScalar());


            Type y = x.GetType();

            if (x>0)
            {
                return UsingTable<Entities.JobQueue, MySqlFetchedJob>(table =>
                {
                    var jobQueue = table.Single(jq => jq.Id == (int) x);
                    return new MySqlFetchedJob(Connection, jobQueue.Id, jobQueue.Id.ToString(), jobQueue.Queue);
                });
            }

            return null;
        }

    

        public void Enqueue(string queue, string jobId)
        {

            queue.Should().NotBeNullOrEmpty();
            jobId.Should().NotBeNullOrEmpty();

            UsingDatabase(db =>
            {
                try
                {

                    {
                        db.Insert(new Entities.JobQueue()
                        {
                            JobId = Convert.ToInt32(jobId),
                            Queue = queue
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });

        }
    }

}
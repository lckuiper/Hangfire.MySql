using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Storage;
using LinqToDB;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    internal class MySqlFetchedJob : DatabaseDependant, IFetchedJob
    {
        private bool _disposed;
        private bool _removedFromQueue;
        private bool _requeued;

        public MySqlFetchedJob(
            MySqlConnection connection,
            int id,
            string jobId,
            string queue)
            :base(connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (queue == null) throw new ArgumentNullException("queue");

            Id = id;
            JobId = jobId;
            Queue = queue;
        }

        public int Id { get; private set; }
        public string JobId { get; private set; }
        public string Queue { get; private set; }

        public void RemoveFromQueue()
        {
            UsingTable<Entities.JobQueue>(table => table.Where(jq => jq.Id == Id).Delete());
            _removedFromQueue = true;
        }

        public void Requeue()
        {
            UsingTable<Entities.JobQueue>(
                table => table.Where(jq => jq.Id == Id).Set(jq => jq.FetchedAt, NullDateTime).Update());
            _requeued = true;
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (!_removedFromQueue && !_requeued)
            {
                Requeue();
            }

            _disposed = true;
        }
    }

}
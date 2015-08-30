using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.MySql.src.Entities;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    public class MySqlJobQueueMonitoringApi : DatabaseDependant, IPersistentJobQueueMonitoringApi
    {
        private readonly MySqlStorageOptions _options;

        public MySqlJobQueueMonitoringApi(MySqlConnection connection, MySqlStorageOptions options)
            :base(connection)
        {
            connection.Should().NotBeNull();
            options.Should().NotBeNull();
            _options = options;
        }

        public IEnumerable<string> GetQueues()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetEnqueuedJobIds(string queue, int @from, int perPage)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> GetFetchedJobIds(string queue, int @from, int perPage)
        {
            throw new NotImplementedException();
        }

        public EnqueuedAndFetchedCount GetEnqueuedAndFetchedCount(string queue)
        {
            throw new NotImplementedException();
        }
    }
}

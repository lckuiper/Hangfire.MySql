using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.MySql.Common;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    public class MySqlJobQueueProvider : ShortConnectingDatabaseActor, IPersistentJobQueueProvider
    {
        private readonly MySqlStorageOptions _options;

        public MySqlJobQueueProvider(string connectionString, MySqlStorageOptions options)
            : base(connectionString)
        {
            options.Should().NotBeNull();
            _options = options;
        }


        public IPersistentJobQueue GetJobQueue(string connectionString)
        {
            return new MySqlJobQueue(connectionString, _options);
        }



        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(string connectionString)
        {
            return new MySqlJobQueueMonitoringApi(connectionString, _options);
        }

      
    }
}

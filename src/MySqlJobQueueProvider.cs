using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    public class MySqlJobQueueProvider : DatabaseDependant, IPersistentJobQueueProvider
    {
        private readonly MySqlStorageOptions _options;

        public MySqlJobQueueProvider(MySqlConnection connection, MySqlStorageOptions options) : base(connection)
        {
            options.Should().NotBeNull();
            _options = options;
        }


        public IPersistentJobQueue GetJobQueue(MySqlConnection connection)
        {
            return new MySqlJobQueue(connection, _options);
        }

       

        public IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(MySqlConnection connection)
        {
            return new MySqlJobQueueMonitoringApi(connection, _options);
        }

      
    }
}

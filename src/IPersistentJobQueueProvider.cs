﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
        public interface IPersistentJobQueueProvider
        {
            IPersistentJobQueue GetJobQueue(string connectionString);
            IPersistentJobQueueMonitoringApi GetJobQueueMonitoringApi(string connectionString);
        }
    
}

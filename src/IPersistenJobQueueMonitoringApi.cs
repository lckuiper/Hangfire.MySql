using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.MySql.src.Entities;

namespace Hangfire.MySql.src
{
    public interface IPersistentJobQueueMonitoringApi
    {
        IEnumerable<string> GetQueues();
        IEnumerable<int> GetEnqueuedJobIds(string queue, int from, int perPage);
        IEnumerable<int> GetFetchedJobIds(string queue, int from, int perPage);
        EnqueuedAndFetchedCount GetEnqueuedAndFetchedCount(string queue);
    }
}

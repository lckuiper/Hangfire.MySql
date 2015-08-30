using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Storage;

namespace Hangfire.MySql.src
{
    public interface IPersistentJobQueue
    {
        IFetchedJob Dequeue(string[] queues, CancellationToken cancellationToken);
        void Enqueue(string queue, string jobId);
    }
}

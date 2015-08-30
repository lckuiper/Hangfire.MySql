using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.Logging;
using Hangfire.Server;

namespace Hangfire.MySql.src
{
    internal class ExpirationManager : IServerComponent
    {
        private readonly MySqlStorage _storage;
        private readonly TimeSpan _checkInterval;

        private static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromHours(1);

        private static readonly TimeSpan DelayBetweenPasses = TimeSpan.FromSeconds(1);
        private const int NumberOfRecordsInSinglePass = 1000;


        public ExpirationManager(MySqlStorage storage)
            : this(storage, DefaultCheckInterval)
        {
        }

        public ExpirationManager(MySqlStorage storage, TimeSpan checkInterval)
        {

            storage.Should().NotBeNull();


            _storage = storage;
            _checkInterval = checkInterval;
        }


        public void Execute(CancellationToken cancellationToken)
        {
            // TODO
        }
    }

}
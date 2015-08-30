using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.MySql.src;
using Hangfire.Server;
using Hangfire.Storage;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql
{
    public class MySqlStorage : JobStorage
    {
        private readonly MySqlStorageOptions _options;
        private string _connectionString;
        private MySqlConnection _existingConnection;


        internal MySqlConnection CreateAndOpenConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }


        public MySqlStorage(string nameOrConnectionString)
            : this(nameOrConnectionString, new MySqlStorageOptions())
        {
        }

        

        public MySqlStorage(string nameOrConnectionString, MySqlStorageOptions options)
        {
            _options = options;

            if (IsConnectionString(nameOrConnectionString))
            {
                _connectionString = nameOrConnectionString;
            }
            else if (IsConnectionStringInConfiguration(nameOrConnectionString))
            {
                _connectionString = ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString;
            }
            else
            {
                throw new ArgumentException(
                    string.Format("Could not find connection string with name '{0}' in application config file",
                                  nameOrConnectionString));
            }



            InitializeQueueProviders();

        }

        public PersistentJobQueueProviderCollection QueueProviders { get; private set; }


        public override IMonitoringApi GetMonitoringApi()
        {
            return new MySqlMonitoringApi(_connectionString, QueueProviders);
        }

        public override IStorageConnection GetConnection()
        {
            var connection = _existingConnection ?? CreateAndOpenConnection();
            return new MySqlStorageConnection(connection,QueueProviders);
        }

        public override IEnumerable<IServerComponent> GetComponents()
        {
            yield return new ExpirationManager(this, TimeSpan.FromHours(1));  // CONFIG !
           // yield return new CountersAggregator(this, _options.CountersAggregateInterval);
        }


        private void InitializeQueueProviders()
        {
            var defaultQueueProvider = new MySqlJobQueueProvider(CreateAndOpenConnection(),_options);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);
        }



        

        private bool IsConnectionString(string nameOrConnectionString)
        {
            return nameOrConnectionString.Contains(";");
        }

        private bool IsConnectionStringInConfiguration(string connectionStringName)
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            return connectionStringSetting != null;
        }

    }

    public static class MySqlStorageExtensions
    {
        public static IGlobalConfiguration<MySqlStorage> UseMySqlStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string nameOrConnectionString)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");

            var storage = new MySqlStorage(nameOrConnectionString);
            return configuration.UseStorage(storage);
        }

        public static IGlobalConfiguration<MySqlStorage> UseMySqlStorage(
            [NotNull] this IGlobalConfiguration configuration,
            [NotNull] string nameOrConnectionString,
            [NotNull] MySqlStorageOptions options)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            if (nameOrConnectionString == null) throw new ArgumentNullException("nameOrConnectionString");
            if (options == null) throw new ArgumentNullException("options");

            var storage = new MySqlStorage(nameOrConnectionString, options);
            return configuration.UseStorage(storage);
        }
    }
}
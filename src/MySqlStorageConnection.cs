﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.Common;
using Hangfire.MySql.Common;
using Hangfire.MySql.src.Entities;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Job = Hangfire.Common.Job;

namespace Hangfire.MySql.src
{

    internal class MySqlStorageConnection : ShortConnectingDatabaseActor, IStorageConnection
    {
        private readonly PersistentJobQueueProviderCollection _queueProviders;

        public MySqlStorageConnection(string connectionString, 
            PersistentJobQueueProviderCollection queueProviders)
            : this(connectionString, queueProviders, true)
        {
        }

        public MySqlStorageConnection(
            string connectionString,
            PersistentJobQueueProviderCollection queueProviders,
            bool ownsConnection)
            : base(connectionString)
        {

            queueProviders.Should().NotBeNull();
            _queueProviders = queueProviders;
        }


      
        public IWriteOnlyTransaction CreateWriteTransaction()
        {
            return new MySqlWriteOnlyTransaction(ConnectionString, _queueProviders);
        }

        public IDisposable AcquireDistributedLock(string resource, TimeSpan timeout)
        {
            return new MySqlDistributedLock(resource, timeout, ConnectionString);
        }

        public string CreateExpiredJob(Job job, IDictionary<string, string> parameters, DateTime createdAt,
            TimeSpan expireIn)
        {

            Debug.Write("CreateExpiredJob ");


            // TODO make this a transaction

            var invocationData = InvocationData.Serialize(job);

            var persistedJob = new Entities.Job()
            {
                InvocationData = JsonConvert.SerializeObject(invocationData),
                Arguments = invocationData.Arguments,
                CreatedAt = createdAt,
                ExpireAt = createdAt.Add(expireIn)
            };


            return UsingDatabase(db =>
            {


                int jobId = Convert.ToInt32(db.InsertWithIdentity(persistedJob));

                foreach (var parameter in parameters)
                {
                    db.Insert(new JobParameter()
                    {
                        JobId = jobId,
                        Name = parameter.Key,
                        Value = parameter.Value
                    });

                }

                Debug.WriteLine("#" + jobId);

                return jobId.ToString(CultureInfo.InvariantCulture);
            });
        }


        public IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken)
        {

            // TODO: THIS WON'T LOOK IN ALL QUEUES ??

            if (queues == null || queues.Length == 0) throw new ArgumentNullException("queues");

            var providers = queues
                .Select(queue => _queueProviders.GetProvider(queue))
                .Distinct()
                .ToArray();

            if (providers.Length != 1)
            {
                throw new InvalidOperationException(String.Format(
                    "Multiple provider instances registered for queues: {0}. You should choose only one type of persistent queues per server instance.",
                    String.Join(", ", queues)));
            }

            var persistentQueue = providers[0].GetJobQueue(ConnectionString);
            var fetchedJob = persistentQueue.Dequeue(queues, cancellationToken);

            Debug.WriteLine("#" + fetchedJob.JobId + " fetched ");

            return fetchedJob;

        }

        public void SetJobParameter(string id, string name, string value)
        {
            Debug.WriteLine("#" + id + " SetJobParameter " + name + "=" + value);

            UsingDatabase(db =>
            {
                int jobId = Convert.ToInt32(id);
                db.GetTable<JobParameter>()
                    .Where(jp => jp.JobId == Convert.ToInt32(id))
                    .Where(jp => jp.Name == name)
                    .Delete();
                db.Insert(new JobParameter() 
                    {JobId = jobId, Name = name, Value = value});
            });
        }

        public string GetJobParameter(string id, string name)
        {

            Debug.WriteLine("#" + id + " GetJobParameter " + name);

            return UsingTable<JobParameter,string>(table => table.Where(jp => jp.JobId == Convert.ToInt32(id)).Where(jp=>jp.Name==name)
                    .Select(jp => jp.Value).FirstOrDefault());
        }

        public JobData GetJobData(string jobId)
        {
            Debug.WriteLine("#" + jobId + " GetJobData");

            return UsingTable<Entities.Job,JobData>(table =>
            {
                Entities.Job persistedJob = table.Single(j => j.Id == Convert.ToInt32(jobId));
                var invocationData = JsonConvert.DeserializeObject<InvocationData>(persistedJob.InvocationData);
                invocationData.Arguments = persistedJob.Arguments;

                var returnValue = new JobData()
                {
                    State = persistedJob.StateName,
                    CreatedAt = persistedJob.CreatedAt
                };

                try
                {
                    returnValue.Job = invocationData.Deserialize();
                }
                catch (JobLoadException ex)
                {
                    returnValue.LoadException = ex;
                }

                return returnValue;

            });

        }

        public StateData GetStateData(string jobId)
        {
            Debug.WriteLine("#" + jobId + " GetStateData");


            return UsingDatabase<StateData>(db =>

            {
                var jobStateId = db.GetTable<Entities.Job>()
                    .Single(j => j.Id == Convert.ToInt32(jobId))
                    .StateId;

                var stateDataString = db.GetTable<Entities.JobState>()
                    .Single(js => js.Id == jobStateId)
                    .Data;

                return JsonConvert.DeserializeObject<StateData>(stateDataString);

            });
        }

        public void AnnounceServer(string serverId, ServerContext context)
        {
            serverId.Should().NotBeNullOrEmpty();
            context.Should().NotBeNull();

            Debug.WriteLine("Announce server " + serverId);


            var data = new ServerData
            {
                WorkerCount = context.WorkerCount,
                Queues = context.Queues,
                StartedAt = DateTime.UtcNow,
            };

            UsingDatabase(db => db.InsertOrReplace(new Entities.Server()
            {
                Id = serverId,
                Data = JsonConvert.SerializeObject(data),
                LastHeartbeat = DateTime.UtcNow
            }));


        }

        public void RemoveServer(string serverId)
        {
            UsingTable<Entities.Server>(table => table.Where(s => s.Id == serverId)
                .Delete());
        }

        public void Heartbeat(string serverId)
        {
            UsingTable<Entities.Server>(table => table.Where(s => s.Id == serverId)
                .Set(s => s.LastHeartbeat, DateTime.UtcNow)
                .Update());
        }

        public int RemoveTimedOutServers(TimeSpan timeOut)
        {
            return UsingTable<Entities.Server,int>(table => table.Delete(s => s.LastHeartbeat < DateTime.UtcNow - timeOut));
        }

        public HashSet<string> GetAllItemsFromSet(string key)
        {
            var values = UsingDatabase(db =>
                db.GetTable<Entities.Set>()
                    .Where(h => h.Key == key)
                    .Select(h => h.Value))
                .ToArray();

            return new HashSet<string>(values);

        }

        public string GetFirstByLowestScoreFromSet(string key, double fromScore, double toScore)
        {
            return UsingTable<Set, string>(table =>

                table.Where(v => v.Key == key)
                    .Where(v => (v.Score >= fromScore) && (v.Score <= toScore))
                    .OrderBy(v => v.Score)
                    .Select(v => v.Value)
                    .FirstOrDefault());
        }


        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            // note: each "key"   as a Field/Value collection

            // TODO: make a transaction

            UsingDatabase(db =>
            {
                foreach (var pair in keyValuePairs)
                {
                    db.GetTable<Hash>().Where(h => (h.Key == key) && (h.Field == pair.Key)).Delete();
                    db.Insert(new Entities.Hash()
                    {
                        Key = key,
                        Field = pair.Key,
                        Value = pair.Value
                    });
                }
            });

        }

        public Dictionary<string, string> GetAllEntriesFromHash(string key)
        {
            return UsingTable<Hash, Dictionary<string, string>>(table =>
                table.Where(h => h.Key == key)
                .ToDictionary(h => h.Field, h => h.Value));

        }


        public void Dispose()
        {
            
        }
    }


}

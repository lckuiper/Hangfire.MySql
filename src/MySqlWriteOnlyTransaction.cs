using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire.MySql.src.Entities;
using Hangfire.States;
using Hangfire.Storage;
using LinqToDB;
using LinqToDB.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Hangfire.MySql.src
{
    public class MySqlWriteOnlyTransaction : DatabaseDependant, IWriteOnlyTransaction
    {
        private readonly PersistentJobQueueProviderCollection _queueProviders;
        private readonly Queue<Action<DataConnection>> _commandQueue
            = new Queue<Action<DataConnection>>();

        private readonly SortedSet<string> _lockedResources = new SortedSet<string>();


        public MySqlWriteOnlyTransaction(
            MySqlConnection connection,
            PersistentJobQueueProviderCollection queueProviders) : base(connection)
        {
            _queueProviders = queueProviders;
            if (connection == null) throw new ArgumentNullException("connection");
            if (queueProviders == null) throw new ArgumentNullException("queueProviders");

        }



        public void ExpireJob(string jobId, TimeSpan expireIn)
        {
            throw new NotImplementedException();
        }

        private readonly DateTime? NullDateTime = null;

        public void PersistJob(string jobId)
        {

            QueueCommand(db =>

                db.GetTable<Entities.Job>()
                    .Where<Job>(j => j.Id == Convert.ToInt32(jobId))
                    .Set(j => j.ExpireAt, NullDateTime)
                    .Update());
   
        }

        public void SetJobState(string jobId, IState state)
        {
            using (var db = CreateDataConnection())
            {
                var jobState = BuildState(jobId, state);


                db.GetTable<Job>().Where(j => j.Id == Convert.ToInt32(jobId))
                    .Set(j => j.StateName, state.Name)
                    .Set(j => j.StateReason, state.Reason)
                    .Set(j => j.StateData, jobState.Data)
                    .Update();
            }

        }

        private JobState BuildState(string jobId, IState state)
        {
            return new JobState()
            {
                JobId = Convert.ToInt32(jobId),  
                CreatedAt = DateTime.UtcNow,
                Data = JsonConvert.SerializeObject(state.SerializeData()),
                Name = state.Name,
                Reason = state.Reason
            };

        }

        public void AddJobState(string jobId, IState state)
        {
            jobId.Should().NotBeNullOrEmpty();
            state.Should().NotBeNull();

            QueueCommand(db =>
            {
                var persistedState = BuildState(jobId, state);
                var stateId = db.InsertWithIdentity(persistedState);
                db.GetTable<Job>().Where(j => j.Id == persistedState.JobId)
                    .Set(j=>j.StateId,stateId)
                    .Set(j => j.StateName, persistedState.Name)
                    .Set(j => j.StateReason, persistedState.Reason)
                    .Set(j => j.StateData, persistedState.Data)
                    .Update();
            });


        }

        public void AddToQueue(string queue, string jobId)
        {
            var provider = _queueProviders.GetProvider(queue);
            var persistentQueue = provider.GetJobQueue(Connection);

            QueueCommand(_ => persistentQueue.Enqueue(queue, jobId));
        }

        public void IncrementCounter(string key)
        {
        }

        public void IncrementCounter(string key, TimeSpan expireIn)
        {
        }

        public void DecrementCounter(string key)
        {
        }

        public void DecrementCounter(string key, TimeSpan expireIn)
        {
        }

        public void AddToSet(string key, string value)
        {
        }

        public void AddToSet(string key, string value, double score)
        {
        }

        public void RemoveFromSet(string key, string value)
        {
        }

        public void InsertToList(string key, string value)
        {
        }

        public void RemoveFromList(string key, string value)
        {
        }

        public void TrimList(string key, int keepStartingFrom, int keepEndingAt)
        {
        }

        public void SetRangeInHash(string key, IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
        }

        public void RemoveHash(string key)
        {
        }

        public void Commit()
        {
            var timeout = TimeSpan.FromSeconds(5);
            var locks =
                _lockedResources.Select(resource => new MySqlDistributedLock(resource, timeout, Connection)).ToList();


            UsingDatabase(db => { foreach (var command in _commandQueue) command(db); });

        }

        private void AcquireListLock()
        {
            AcquireLock(String.Format("Hangfire:List:Lock"));
        }

        private void AcquireSetLock()
        {
            AcquireLock(String.Format("Hangfire:Set:Lock"));
        }

        private void AcquireHashLock()
        {
            AcquireLock(String.Format("Hangfire:Hash:Lock"));
        }

        private void AcquireLock(string resource)
        {
            _lockedResources.Add(resource);
        }

        internal void QueueCommand(Action<DataConnection> action)
        {
            _commandQueue.Enqueue(action);
        }

        public void Dispose()
        {
            // TODO
            //throw new NotImplementedException();
        }
    }
}

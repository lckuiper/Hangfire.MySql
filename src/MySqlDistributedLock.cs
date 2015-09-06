using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.MySql.src.Entities;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    internal class MySqlDistributedLock : IDisposable
    {
        private readonly string _resource;
        private readonly MySqlConnection _connection;
        private static readonly TimeSpan WaitBetweenAttempts = TimeSpan.FromSeconds(1);
        private int _lockId;

        public MySqlDistributedLock(string resource, TimeSpan timeout, string connectionString)
        {
            _resource = resource;
            _connection = new MySqlConnection(connectionString);
            var tooLateTime = DateTime.UtcNow + timeout;

            while (true)
            {
                _lockId = InsertRow();
                if (_lockId != 0)
                    break;

                Thread.Sleep(WaitBetweenAttempts);

                if (DateTime.UtcNow > tooLateTime)
                {
                    throw new MySqlDistributedLockException("Timeout expired while trying to get lock on '" + resource + "'");
                }


            }

            Debug.WriteLine("acquired MySqlDistributedLock " + _resource);



        }

        protected int InsertRow()
        {
            try
            {
                using (var db = new DataConnection(new MySqlDataProvider(), _connection))
                {
                    return Convert.ToInt32(db.InsertWithIdentity(new Entities.DistributedLock()
                    {
                        Resource = _resource
                    }));
                }
            }
            catch (Exception)
            {
                return 0;
            }

        }

        private bool _lockDeleted = false;

        public void Dispose()
        {
            Debug.WriteLine("disposing MySqlDistributedLock " + _resource);

            if (!_lockDeleted)
            {
                _lockDeleted = true;
                using (var db = new DataConnection(new MySqlDataProvider(), _connection))
                {
                    int nDeleted = db.GetTable<DistributedLock>().Where(dl => dl.Id == _lockId).Delete();
                    if (nDeleted != 1)
                        throw new MySqlDistributedLockException("Lock " + _lockId + " on reousrce " + _resource +
                                                                " disappeared");

                }
            }

        }
    }
}

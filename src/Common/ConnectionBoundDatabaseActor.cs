using System;
using System.Diagnostics;
using FluentAssertions;
using Hangfire.MySql.src;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.Common
{
    public class ConnectionBoundDatabaseActor : DatabaseActor, IDisposable
    {
        protected ConnectionBoundDatabaseActor(MySqlConnection connection, bool ownsConnection=false)
        {
            connection.Should().NotBeNull();
            Connection = connection;
            OwnsConnection = ownsConnection;
        }

        protected MySqlConnection Connection { get; private set; }

        protected bool OwnsConnection { get; private set; }

        protected override TResult Invoke<TResult>(Func<DataConnection, TResult> func)
        {
            Debug.WriteLine(func.ToString());

            using (var dataConnection = new DataConnection(new MySqlDataProvider(), Connection))
            {
                return func(dataConnection);
            }
        }


        public void Dispose()
        {
            if (OwnsConnection)
            {
                Connection.Dispose();
            }
        }
    }
}

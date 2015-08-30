using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using MySql.Data.MySqlClient;

namespace Hangfire.MySql.src
{
    public abstract class DatabaseDependant
    {
        public MySqlConnection Connection { get; private set; }

        protected readonly DateTime? NullDateTime = null;

        protected DatabaseDependant(MySqlConnection connection)
        {
            Connection = connection;
        }



        protected void UsingTable<TEntity>(Action<ITable<TEntity>> action) where TEntity : class
        {
            UsingDatabase(db => action(db.GetTable<TEntity>()));
        }

        protected TResult UsingTable<TEntity, TResult>(Func<ITable<TEntity>, TResult> action) where TEntity : class
        {
            return UsingDatabase(db => action(db.GetTable<TEntity>()));
        }


        protected
            void UsingDatabase(Action<DataConnection> action)
        {
            try
            {

                using (var db = CreateDataConnection())
                {
                    action(db);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;

            }
        }

        protected TResult UsingDatabase<TResult>(Func<DataConnection, TResult> func)
        {
            try
            {

                using (var db = CreateDataConnection())
                {
                    return func(db);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;

            }

        }

        protected DataConnection CreateDataConnection()
        {
            return new DataConnection(new MySqlDataProvider(), Connection);
        }
    }
}

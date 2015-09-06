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
    public abstract class DatabaseActor
    {
       
        protected readonly DateTime? NullDateTime = null;

        


        protected void UsingTable<TEntity>(Action<ITable<TEntity>> action) where TEntity : class
        {
            UsingDatabase(db => action(db.GetTable<TEntity>()));
        }

        protected TResult UsingTable<TEntity, TResult>(Func<ITable<TEntity>, TResult> action) where TEntity : class
        {
            return UsingDatabase(db => action(db.GetTable<TEntity>()));
        }


        protected void UsingDatabase(Action<DataConnection> action)
        {
            UsingDatabase(dc => { action(dc); return true; });
        }

        protected TResult UsingDatabase<TResult>(Func<DataConnection, TResult> func)
        {
            return Invoke(func);
        }
        

        

        protected abstract TResult Invoke<TResult>(Func<DataConnection, TResult> func);

    }
}

using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.Data.Sqlite;
using System;
using System.Threading.Tasks;
using Compze.Core.Configuration.Internal;

namespace Compze.Sql.Sqlite;

public static class SqliteConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.SqliteProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   static IComponentRegistrar SqliteProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName))));
}


internal interface ISqliteConnectionPool : IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>
{
   static ISqliteConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static SqliteConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class SqliteConnectionPool : ISqliteConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>> _pool;

      public SqliteConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeSqliteConnection, SqliteCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<ICompzeSqliteConnection, SqliteCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.MustUseSameConnectionThroughoutATransaction,
                  ICompzeSqliteConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeSqliteConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeSqliteConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}

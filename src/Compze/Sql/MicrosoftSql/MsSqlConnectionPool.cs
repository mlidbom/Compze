using Compze.Sql.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Compze.Core.Configuration.Internal;

namespace Compze.Sql.MicrosoftSql;

public static class MsSqlSqlConnectionPoolRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar MsSqlConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         return registrar.Register(
            Singleton.For<IMsSqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName))));
      }
   }
}


internal interface IMsSqlConnectionPool : IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>
{
   static IMsSqlConnectionPool CreateInstance(string connectionString) => CreateInstance(() => connectionString);
   static MsSqlConnectionPool CreateInstance(Func<string> getConnectionString) => new(getConnectionString);

   class MsSqlConnectionPool : IMsSqlConnectionPool
   {
      readonly LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>> _pool;

      public MsSqlConnectionPool(Func<string> getConnectionString)
      {
         _pool = new LazyCE<IDbConnectionPool<ICompzeMsSqlConnection, SqlCommand>>(
            () =>
            {
               var connectionString = getConnectionString();
               return DbConnectionManager<ICompzeMsSqlConnection, SqlCommand>.ForConnectionString(
                  connectionString,
                  PoolableConnectionFlags.Defaults,
                  ICompzeMsSqlConnection.Create);
            });
      }

      public override string ToString() => _pool.ValueIfInitialized()?.ToString() ?? "Not initialized";
      public TResult UseConnection<TResult>(Func<ICompzeMsSqlConnection, TResult> func) => _pool.Value.UseConnection(func);
      public async Task<TResult> UseConnectionAsync<TResult>(Func<ICompzeMsSqlConnection, Task<TResult>> func) => await _pool.Value.UseConnectionAsync(func).caf();
   }
}

using Compze.Common.Configuration;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.Sqlite;

namespace Compze.Tessaging.Hosting.Sql.Sqlite;

public static class SqliteMemorySqlLayerRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar SqliteMemoryConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         throw new InvalidOperationException("SqliteMemory is only supported in testing mode");
      }

      return registrar;
   }

   public static IComponentRegistrar SqliteMemoryDbPoolAndConnectionPoolForConnectionStringNameIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.SqliteMemoryDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteMemoryDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

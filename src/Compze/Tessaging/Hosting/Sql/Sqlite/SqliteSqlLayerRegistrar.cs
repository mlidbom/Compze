using Compze.Common.Configuration;
using Compze.Sql.Sqlite;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.Sqlite;

namespace Compze.Tessaging.Hosting.Sql.Sqlite;

public static class SqliteSqlLayerRegistrar
{
   public static IComponentRegistrar SqliteConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.DbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
      } else
      {
         registrar.SqliteProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IComponentRegistrar DbPoolAndConnectionPoolForRandomConnectionString(this IComponentRegistrar registrar)
      => registrar.DbPoolAndConnectionPoolForConnectionStringName(Guid.NewGuid().ToString());

   static IComponentRegistrar SqliteProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   static IComponentRegistrar DbPoolAndConnectionPoolForConnectionStringName(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.SqliteDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }

   public static IComponentRegistrar SqliteMemoryConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.SqliteMemoryDbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
      } else
      {
         throw new InvalidOperationException("SqliteMemory is only supported in testing mode");
      }

      return registrar;
   }

   public static IComponentRegistrar SqliteMemoryDbPoolAndConnectionPoolForConnectionStringName(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.SqliteMemoryDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteMemoryDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

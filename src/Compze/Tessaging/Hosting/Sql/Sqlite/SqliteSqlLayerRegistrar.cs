using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.Sqlite;

namespace Compze.Tessaging.Hosting.Sql.Sqlite;

public static class SqliteSqlLayerRegistrar
{
   public static IDependencyRegistrar SqliteConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
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

   public static IDependencyRegistrar DbPoolAndConnectionPoolForRandomConnectionString(this IDependencyRegistrar registrar)
      => registrar.DbPoolAndConnectionPoolForConnectionStringName(Guid.NewGuid().ToString());

   public static IDependencyRegistrar SqliteProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => ISqliteConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   public static IDependencyRegistrar DbPoolAndConnectionPoolForConnectionStringName(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.Register(Singleton.For<SqliteDbPool>()
                                  .CreatedBy((IConfigurationParameterProvider _) => new SqliteDbPool())
                                  .DelegateToParentServiceLocatorWhenCloning());

      return registrar.Register(
         Singleton.For<ISqliteConnectionPool>()
                  .CreatedBy((SqliteDbPool pool) => ISqliteConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

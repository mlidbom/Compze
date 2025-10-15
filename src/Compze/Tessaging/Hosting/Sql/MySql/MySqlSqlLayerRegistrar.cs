using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MySql;

namespace Compze.Tessaging.Hosting.Sql.MySql;

public static class MySqlSqlLayerRegistrar
{
   public static IDependencyRegistrar MySqlConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.MySqlDbPoolWithConnectionPool(connectionStringName);
      } else
      {
         registrar.MySqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IDependencyRegistrar MySqlDbPoolWithConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.MySqlDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
      );
   }

   public static IDependencyRegistrar MySqlProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());
   }
}

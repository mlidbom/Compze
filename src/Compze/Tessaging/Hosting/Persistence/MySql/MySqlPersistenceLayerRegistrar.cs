using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MySql;

namespace Compze.Tessaging.Hosting.Persistence.MySql;

public static class MySqlPersistenceLayerRegistrar
{
   public static IDependencyRegistrar MySqlConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.MySqlProductionConnectionPool(connectionStringName);
      } else
      {
         registrar.MySqlDbPoolWithConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IDependencyRegistrar MySqlProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.Register(Singleton.For<MySqlDbPool>()
                                  .CreatedBy((IConfigurationParameterProvider _) => new MySqlDbPool())
                                  .DelegateToParentServiceLocatorWhenCloning());

      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
      );
   }

   public static IDependencyRegistrar MySqlNewDbPoolWithConnectionPool(this IDependencyRegistrar registrar) =>
      registrar.MySqlDbPoolWithConnectionPool(Guid.NewGuid().ToString());

   public static IDependencyRegistrar MySqlDbPoolWithConnectionPool(this IDependencyRegistrar registrar, string connectionStringName) =>
      registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());
}

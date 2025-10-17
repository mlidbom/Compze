using Compze.Common.Configuration;
using Compze.Sql.MySql.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MySql;

namespace Compze.Tessaging.Hosting.Sql.MySql;

public static class MySqlSqlLayerRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   public static IComponentRegistrar MySqlConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.MySqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IComponentRegistrar MySqlDbPoolWithConnectionPoolForConnectionStringName(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.MySqlDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
      );
   }

   public static IComponentRegistrar MySqlProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      return registrar.Register(
         Singleton.For<IMySqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());
   }
}

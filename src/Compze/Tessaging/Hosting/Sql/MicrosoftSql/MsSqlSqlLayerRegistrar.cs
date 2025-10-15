using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.MicrosoftSql;

namespace Compze.Tessaging.Hosting.Sql.MicrosoftSql;

public static class MsSqlSqlLayerRegistrar
{
   public static IDependencyRegistrar MsSqlConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.DbPoolAndConnectionPoolForConnectionStringName(connectionStringName);
      } else
      {
         registrar.MsSqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IDependencyRegistrar MsSqlProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
      => registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   public static IDependencyRegistrar DbPoolAndConnectionPoolForConnectionStringName(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.MsSqlDbPoolIfNotAlreadyRegistered();

      return registrar.Register(
         Singleton.For<IMsSqlConnectionPool>()
                  .CreatedBy((MsSqlDbPool pool) => IMsSqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName))));
   }
}

using Compze.Common.Configuration;
using Compze.Sql.PostgreSql;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.PostgreSql;

namespace Compze.Tessaging.Hosting.Sql.PostgreSql;

public static class PgSqlSqlLayerRegistrar
{
   public interface ITestingRegistrar
   {
      public IComponentRegistrar Register(string connectionStringName);
   }

   internal static void RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(this IEndpointBuilder @this) =>
      @this.Container.Register().PgSqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static IComponentRegistrar PgSqlConnectionPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.TryGetTestingRegistrar<ITestingRegistrar>() is {} testingRegistrar)
      {
         return testingRegistrar.Register(connectionStringName);
      } else
      {
         registrar.PgSqlProductionConnectionPool(connectionStringName);
      }

      return registrar;
   }

   public static IComponentRegistrar PgSqlProductionConnectionPool(this IComponentRegistrar registrar, string connectionStringName) =>
      registrar.Register(
         Singleton.For<IPgSqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IPgSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   public static IComponentRegistrar PgSqlNewDbPoolWithConnectionPool(this IComponentRegistrar registrar) =>
      registrar.PgSqlDbPoolWithConnectionPoolIfNotAlreadyRegistered(Guid.NewGuid().ToString());

   public static IComponentRegistrar PgSqlDbPoolWithConnectionPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.PgSqlDbPoolIfNotAlreadyRegistered();

      registrar.Register(
         Singleton.For<IPgSqlConnectionPool>()
                  .CreatedBy((PgSqlDbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));

      return registrar;
   }
}

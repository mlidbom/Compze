using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.PostgreSql;

namespace Compze.Tessaging.Hosting.Sql.PostgreSql;

public static class PgSqlSqlLayerRegistrar
{
   internal static void RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(this IEndpointBuilder @this) =>
      @this.Container.Register().PgSqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static IComponentRegistrar PgSqlConnectionPoolIfNotAlreadyRegistered(this IComponentRegistrar registrar, string connectionStringName)
   {
      if(registrar.RunMode.IsTesting)
      {
         registrar.PgSqlDbPoolWithConnectionPool(connectionStringName);
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
      registrar.PgSqlDbPoolWithConnectionPool(Guid.NewGuid().ToString());

   public static IComponentRegistrar PgSqlDbPoolWithConnectionPool(this IComponentRegistrar registrar, string connectionStringName)
   {
      registrar.PgSqlDbPoolIfNotAlreadyRegistered();

      registrar.Register(
         Singleton.For<IPgSqlConnectionPool>()
                  .CreatedBy((PgSqlDbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));

      return registrar;
   }
}

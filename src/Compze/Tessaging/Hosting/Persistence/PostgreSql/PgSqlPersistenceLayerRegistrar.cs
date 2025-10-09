using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Testing.DbPool.PostgreSql;

namespace Compze.Tessaging.Hosting.Persistence.PostgreSql;

public static class PgSqlPersistenceLayerRegistrar
{
   internal static void RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(this IEndpointBuilder @this) =>
      @this.Container.Register().PgSqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static IDependencyRegistrar PgSqlConnectionPoolIfNotAlreadyRegistered(this IDependencyRegistrar registrar, string connectionStringName)
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

   public static IDependencyRegistrar PgSqlProductionConnectionPool(this IDependencyRegistrar registrar, string connectionStringName) =>
      registrar.Register(
         Singleton.For<IPgSqlConnectionPool>()
                  .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IPgSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                  .DelegateToParentServiceLocatorWhenCloning());

   public static IDependencyRegistrar PgSqlNewDbPoolWithConnectionPool(this IDependencyRegistrar registrar) =>
      registrar.PgSqlDbPoolWithConnectionPool(Guid.NewGuid().ToString());

   public static IDependencyRegistrar PgSqlDbPoolWithConnectionPool(this IDependencyRegistrar registrar, string connectionStringName)
   {
      registrar.Register(Singleton.For<PgSqlDbPool>()
                                  .CreatedBy((IConfigurationParameterProvider _) => new PgSqlDbPool())
                                  .DelegateToParentServiceLocatorWhenCloning());

      registrar.Register(
         Singleton.For<IPgSqlConnectionPool>()
                  .CreatedBy((PgSqlDbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName))));

      return registrar;
   }
}

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
      @this.Container.RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(@this.Configuration.ConnectionStringName);

   public static IDependencyRegistrar PgSqlConnectionPoolIfNotAlreadyRegistered(this IDependencyRegistrar registrar, string connectionStringName)
   {
        registrar.Container().RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(connectionStringName);
        return registrar;
    }

    public static void RegisterPgSqlConnectionPoolIfNotAlreadyRegistered(this IDependencyInjectionContainer container, string connectionStringName)
   {
      if(container.IsRegistered<IPgSqlConnectionPool>()) return;

      //Connection management
      if(container.RunMode.IsTesting)
      {
         container.Register(Singleton.For<PgSqlDbPool>()
                                     .CreatedBy((IConfigurationParameterProvider _) => new PgSqlDbPool())
                                     .DelegateToParentServiceLocatorWhenCloning());

         container.Register(
            Singleton.For<IPgSqlConnectionPool>()
                     .CreatedBy((PgSqlDbPool pool) => IPgSqlConnectionPool.CreateInstance1(() => pool.ConnectionStringFor(connectionStringName)))
         );
      } else
      {
         container.Register(
            Singleton.For<IPgSqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IPgSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                     .DelegateToParentServiceLocatorWhenCloning());
      }
   }
}
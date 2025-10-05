using Compze.Configuration.Abstractions;
using Compze.DependencyInjection;
using Compze.DocumentDb.PostgreSql;
using Compze.EventStore.PostgreSql;
using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Persistence.PostgreSql.Testing;
using Compze.Tessaging.Buses;
using Compze.Tessaging.PostgreSql;

namespace Compze.Persistence.PostgreSql.DependencyInjection;

public static class PgSqlPersistenceLayerRegistrar
{
   public static void RegisterPgSqlPersistenceLayer(this IEndpointBuilder @this) =>
      @this.Container.RegisterPgSqlPersistenceLayer(@this.Configuration.ConnectionStringName);

   public static void RegisterPgSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
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

      //Register individual components
      container.RegisterPgSqlTessaging();
      container.RegisterPgSqlDocumentDb();
      container.RegisterPgSqlEventStore();
   }
}
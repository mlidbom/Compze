using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.Persistence.PgSql.DocumentDb;
using Compze.Persistence.PgSql.EventStore;
using Compze.Persistence.PgSql.Messaging.Buses.Implementation;
using Compze.Persistence.PgSql.SystemExtensions;
using Compze.Refactoring.Naming;
using Compze.SystemCE.ConfigurationCE;
using Compze.Testing.Persistence.PgSql;

namespace Compze.Persistence.PgSql.DependencyInjection;

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

      //Service bus
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlInboxPersistenceLayer(endpointSqlConnection)));

      //DocumentDB
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider) => new PgSqlDocumentDbPersistenceLayer(connectionProvider)));


      //Event store
      container.Register(
         Singleton.For<PgSqlEventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((PgSqlEventStoreConnectionManager connectionManager, ITypeMapper _) => new PgSqlEventStorePersistenceLayer(connectionManager)));
   }
}
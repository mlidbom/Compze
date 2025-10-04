using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Configuration.Abstractions;
using Compze.DependencyInjection;
using Compze.DocumentDb.MicrosoftSql;
using Compze.EventStore.PersistenceLayer.Abstractions;
using Compze.Persistence.MicrosoftSql.EventStore;
using Compze.Persistence.MicrosoftSql.Messaging.Buses.Implementation;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Persistence.MicrosoftSql.Testing;
using Compze.Tessaging.Buses;
using Compze.Tessaging.Buses.Implementation;

namespace Compze.Persistence.MicrosoftSql.DependencyInjection;

public static class MsSqlPersistenceLayerRegistrar
{
   public static void RegisterMsSqlPersistenceLayer(this IEndpointBuilder @this) =>
      @this.Container.RegisterMsSqlPersistenceLayer(@this.Configuration.ConnectionStringName);

   //todo: does the fact that we register all this stuff using a connectionStringName mean that, using named components, we could easily have multiple registrations as long as they use different connectionStrings
   public static void RegisterMsSqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
      //Connection management
      if(container.RunMode.IsTesting)
      {
         container.Register(Singleton.For<MsSqlDbPool>()
                                     .CreatedBy((IConfigurationParameterProvider _) => new MsSqlDbPool())
                                     .DelegateToParentServiceLocatorWhenCloning());

         container.Register(
            Singleton.For<IMsSqlConnectionPool>()
                     .CreatedBy((MsSqlDbPool pool) => IMsSqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
         );
      } else
      {
         container.Register(
            Singleton.For<IMsSqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMsSqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                     .DelegateToParentServiceLocatorWhenCloning());
      }


      //Service bus
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlInboxPersistenceLayer(endpointSqlConnection)));

      //DocumentDB
      container.RegisterMsSqlDocumentDb();

      //Event store
      container.Register(
         Singleton.For<MsSqlEventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MsSqlEventStoreConnectionManager connectionManager, ITypeMapper _) => new MsSqlEventStorePersistenceLayer(connectionManager)));
   }
}

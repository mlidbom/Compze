﻿using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.Persistence.MsSql.DocumentDb;
using Compze.Persistence.MsSql.EventStore;
using Compze.Persistence.MsSql.Messaging.Buses.Implementation;
using Compze.Persistence.MsSql.SystemExtensions;
using Compze.Refactoring.Naming;
using Compze.SystemCE.ConfigurationCE;
using Compze.Testing.Persistence.MsSql;

namespace Compze.Persistence.MsSql.DependencyInjection;

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
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbPersistenceLayer(connectionProvider)));

      //Event store
      container.Register(
         Singleton.For<MsSqlEventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MsSqlEventStoreConnectionManager connectionManager, ITypeMapper _) => new MsSqlEventStorePersistenceLayer(connectionManager)));
   }
}
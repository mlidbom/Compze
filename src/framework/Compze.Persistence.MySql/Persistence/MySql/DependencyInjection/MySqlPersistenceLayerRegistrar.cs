using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.Messaging.Buses.Implementation;
using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.Persistence.MySql.DocumentDb;
using Compze.Persistence.MySql.EventStore;
using Compze.Persistence.MySql.Messaging.Buses.Implementation;
using Compze.Persistence.MySql.SystemExtensions;
using Compze.Refactoring.Naming;
using Compze.SystemCE.ConfigurationCE;
using Compze.Testing.Persistence.MySql;

namespace Compze.Persistence.MySql.DependencyInjection;

public static class MySqlPersistenceLayerRegistrar
{
   public static void RegisterMySqlPersistenceLayer(this IEndpointBuilder @this) =>
      @this.Container.RegisterMySqlPersistenceLayer(@this.Configuration.ConnectionStringName);

   public static void RegisterMySqlPersistenceLayer(this IDependencyInjectionContainer container, string connectionStringName)
   {
      //Connection management
      if(container.RunMode.IsTesting)
      {
         container.Register(Singleton.For<MySqlDbPool>()
                                     .CreatedBy((IConfigurationParameterProvider _) => new MySqlDbPool())
                                     .DelegateToParentServiceLocatorWhenCloning());

         container.Register(
            Singleton.For<IMySqlConnectionPool>()
                     .CreatedBy((MySqlDbPool pool) => IMySqlConnectionPool.CreateInstance(() => pool.ConnectionStringFor(connectionStringName)))
         );
      } else
      {
         container.Register(
            Singleton.For<IMySqlConnectionPool>()
                     .CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => IMySqlConnectionPool.CreateInstance(configurationParameterProvider.GetString(connectionStringName)))
                     .DelegateToParentServiceLocatorWhenCloning());
      }

      //Service bus
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlInboxPersistenceLayer(endpointSqlConnection)));

      //DocumentDB
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));


      //Event store
      container.Register(
         Singleton.For<MySqlEventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MySqlEventStoreConnectionManager connectionManager, ITypeMapper _) => new MySqlEventStorePersistenceLayer(connectionManager)));
   }
}
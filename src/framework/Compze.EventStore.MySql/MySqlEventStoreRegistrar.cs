using Compze.DependencyInjection;
using Compze.EventStore.PersistenceLayer.Abstractions;
using Compze.Persistence.MySql.Infrastructure;

namespace Compze.EventStore.MySql;

public static class MySqlEventStoreRegistrar
{
   public static void RegisterMySqlEventStore(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<MySqlEventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MySqlEventStoreConnectionManager connectionManager) => new MySqlEventStorePersistenceLayer(connectionManager)));
   }
}

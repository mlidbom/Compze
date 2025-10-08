using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.MySql;

public static class MySqlEventStoreRegistrar
{
   public static IDependencyRegistrar MySqlEventStore(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<MySqlEventStoreConnectionManager>()
                  .CreatedBy((IMySqlConnectionPool sqlConnectionProvider) => new MySqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MySqlEventStoreConnectionManager connectionManager) => new MySqlEventStorePersistenceLayer(connectionManager)));
}

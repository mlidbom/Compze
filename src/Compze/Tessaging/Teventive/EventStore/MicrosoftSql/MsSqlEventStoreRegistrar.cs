using Compze.DependencyInjection;
using Compze.EventStore.PersistenceLayer.Abstractions;
using Compze.Persistence.MicrosoftSql.Infrastructure;

namespace Compze.EventStore.MicrosoftSql;

public static class MsSqlEventStoreRegistrar
{
   public static void RegisterMsSqlEventStore(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<MsSqlEventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MsSqlEventStoreConnectionManager connectionManager) => new MsSqlEventStorePersistenceLayer(connectionManager)));
   }
}

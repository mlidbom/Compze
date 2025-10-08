using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.MicrosoftSql;

public static class MsSqlEventStoreRegistrar
{
   public static IDependencyRegistrar MsSqlEventStore(this IDependencyRegistrar registrar)
   {
      registrar.Container().RegisterMsSqlEventStore();
      return registrar;
   }

   public static void RegisterMsSqlEventStore(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<MsSqlEventStoreConnectionManager>()
                  .CreatedBy((IMsSqlConnectionPool sqlConnectionProvider) => new MsSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((MsSqlEventStoreConnectionManager connectionManager) => new MsSqlEventStorePersistenceLayer(connectionManager)));
   }
}

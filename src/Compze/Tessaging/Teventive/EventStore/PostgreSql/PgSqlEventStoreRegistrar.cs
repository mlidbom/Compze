using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.PostgreSql;

public static class PgSqlEventStoreRegistrar
{
   public static IDependencyRegistrar PgSqlEventStore(this IDependencyRegistrar registrar)
   {
      registrar.Container().RegisterPgSqlEventStore();
      return registrar;
    }

    public static void RegisterPgSqlEventStore(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<PgSqlEventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((PgSqlEventStoreConnectionManager connectionManager) => new PgSqlEventStorePersistenceLayer(connectionManager)));
   }
}

using Compze.DependencyInjection;
using Compze.EventStore.PersistenceLayer.Abstractions;
using Compze.Persistence.PostgreSql.Infrastructure;

namespace Compze.EventStore.PostgreSql;

public static class PgSqlEventStoreRegistrar
{
   public static void RegisterPgSqlEventStore(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<PgSqlEventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStorePersistenceLayer>()
                  .CreatedBy((PgSqlEventStoreConnectionManager connectionManager) => new PgSqlEventStorePersistenceLayer(connectionManager)));
   }
}

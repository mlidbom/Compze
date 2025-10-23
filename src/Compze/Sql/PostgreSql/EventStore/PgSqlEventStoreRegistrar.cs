using Compze.Sql.Common.EventStore.Abstractions;
using Compze.Sql.PostgreSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.PostgreSql;

public static class PgSqlEventStoreRegistrar
{
   public static IComponentRegistrar PgSqlEventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<PgSqlEventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStoreSqlLayer>()
                  .CreatedBy((PgSqlEventStoreConnectionManager connectionManager) => new PgSqlEventStoreSqlLayer(connectionManager)));
}

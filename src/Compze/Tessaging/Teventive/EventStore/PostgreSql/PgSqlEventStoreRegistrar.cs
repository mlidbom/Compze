using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.PostgreSql;

public static class PgSqlEventStoreRegistrar
{
   public static IDependencyRegistrar PgSqlEventStore(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<PgSqlEventStoreConnectionManager>()
                  .CreatedBy((IPgSqlConnectionPool sqlConnectionProvider) => new PgSqlEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStoreSqlLayer>()
                  .CreatedBy((PgSqlEventStoreConnectionManager connectionManager) => new PgSqlEventStoreSqlLayer(connectionManager)));
}

using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.Sqlite;

public static class SqliteEventStoreRegistrar
{
   public static IComponentRegistrar SqliteEventStore(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<SqliteEventStoreConnectionManager>()
                  .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteEventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<IEventStoreSqlLayer>()
                  .CreatedBy((SqliteEventStoreConnectionManager connectionManager) => new SqliteEventStoreSqlLayer(connectionManager)));
}

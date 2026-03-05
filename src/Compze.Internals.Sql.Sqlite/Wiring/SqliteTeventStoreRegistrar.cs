using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Private.TEventStore;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteTeventStoreRegistrar
{
   public static IComponentRegistrar SqliteTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<SqliteTeventStoreConnectionManager>()
                            .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((SqliteTeventStoreConnectionManager connectionManager, SqliteSqlLayerSchemaManager schemaManager) => new SqliteTeventStoreSqlLayer(connectionManager, schemaManager)))
               .SqliteSqlLayerSchemaManager();
}

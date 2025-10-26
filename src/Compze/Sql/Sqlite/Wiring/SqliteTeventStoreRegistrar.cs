using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Sql.Sqlite.Private;
using Compze.Sql.Sqlite.Private.TEventStore;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteTeventStoreRegistrar
{
   public static IComponentRegistrar SqliteTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<SqliteTeventStoreConnectionManager>()
                            .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteTeventStoreConnectionManager(sqlConnectionProvider)),
                   Singleton.For<ITeventStoreSqlLayer>()
                            .CreatedBy((SqliteTeventStoreConnectionManager connectionManager, SqliteSqlLayerSchemaManager schemaManager) => new SqliteTeventStoreSqlLayer(connectionManager, schemaManager)))
               .SqliteSqlLayerSchemaManager();
}

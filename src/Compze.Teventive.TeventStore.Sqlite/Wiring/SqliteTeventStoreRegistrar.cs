using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.Teventive.TeventStore.Sqlite._private.SqliteTeventStoreSqlLayer;
using Compze.Sql.Sqlite.Wiring._internal;
using Compze.Teventive.TeventStore.Sqlite._private;

namespace Compze.Teventive.TeventStore.Sqlite.Wiring;

public static class SqliteTeventStoreRegistrar
{
   public static IComponentRegistrar SqliteTeventStoreSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<SqliteTeventStoreConnectionManager>()
                  .CreatedBy((ISqliteConnectionPool sqlConnectionProvider) => new SqliteTeventStoreConnectionManager(sqlConnectionProvider)),
         Singleton.For<ITeventStoreSqlLayer>()
                  .CreatedBy((SqliteTeventStoreConnectionManager connectionManager, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionManager, schemaManager, typeIdInterner)));
}

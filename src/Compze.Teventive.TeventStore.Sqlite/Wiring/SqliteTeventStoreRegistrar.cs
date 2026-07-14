using Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.Tessaging.Teventive.TeventStore.Sqlite.SqliteTeventStoreSqlLayer;

namespace Compze.Tessaging.Teventive.TeventStore.Sqlite.Wiring;

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

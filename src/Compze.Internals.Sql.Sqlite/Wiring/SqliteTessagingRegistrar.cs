using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Private.Tessaging;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteTessagingRegistrar
{
   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager)))
               .SqliteSqlLayerSchemaManager();
}

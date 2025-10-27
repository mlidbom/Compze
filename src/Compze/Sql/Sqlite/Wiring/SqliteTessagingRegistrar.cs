using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.Sqlite.Private;
using Compze.Sql.Sqlite.Private.Tessaging;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager)))
               .SqliteSqlLayerSchemaManager();
}

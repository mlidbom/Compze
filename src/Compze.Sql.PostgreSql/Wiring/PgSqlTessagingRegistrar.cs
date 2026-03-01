using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.PostgreSql.Private;
using Compze.Sql.PostgreSql.Private.Tessaging;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

internal static class PgSqlTessagingRegistrar
{
   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}

using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Private.Tessaging;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

static class PgSqlTessagingRegistrar
{
   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
                   Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                            .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager)),
                   Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                            .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager)))
               .PgSqlSqlLayerSchemaManager();
}

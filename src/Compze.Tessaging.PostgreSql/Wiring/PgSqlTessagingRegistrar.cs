using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.PostgreSql.Wiring;

public static class PgSqlTessagingRegistrar
{
   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlSchemaContribution(PgSqlInboxSqlLayer.SchemaCreationSql)
               .PgSqlSchemaContribution(PgSqlOutboxSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}

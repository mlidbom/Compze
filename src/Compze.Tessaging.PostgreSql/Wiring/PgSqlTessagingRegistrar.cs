using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.PostgreSql.Wiring;
using Compze.Tessaging._internal.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Compze.Sql.PostgreSql.Wiring._internal;
using Compze.Tessaging.PostgreSql._private;

namespace Compze.Tessaging.PostgreSql.Wiring;

public static class PgSqlTessagingRegistrar
{
   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlTypeIdInterner()
               .PgSqlSchemaContribution((EndpointTableSet tables) => PgSqlInboxSqlLayer.SchemaCreationSql(tables))
               .PgSqlSchemaContribution((EndpointTableSet tables) => PgSqlOutboxSqlLayer.SchemaCreationSql(tables))
               .PgSqlSchemaContribution((EndpointTableSet tables) => PgSqlPeerRegistrySqlLayer.SchemaCreationSql(tables))
               .PgSqlSchemaContribution(PgSqlEndpointCatalogSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IEndpointCatalogSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager) => new PgSqlEndpointCatalogSqlLayer(endpointSqlConnection, schemaManager)),
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, EndpointTableSet tables) => new PgSqlPeerRegistrySqlLayer(endpointSqlConnection, schemaManager, tables)));
}

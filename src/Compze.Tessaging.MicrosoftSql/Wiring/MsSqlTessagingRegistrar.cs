using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Tessaging._internal.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;
using Compze.Internals.Sql.MicrosoftSql.Wiring._internal;
using Compze.Tessaging.MicrosoftSql._private;
using Compze.Internals.Sql.MicrosoftSql._internal;

namespace Compze.Tessaging.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   public static IComponentRegistrar MsSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlTypeIdInterner()
               .MsSqlSchemaContribution((EndpointTableSet tables) => MsSqlInboxSqlLayer.SchemaCreationSql(tables))
               .MsSqlSchemaContribution((EndpointTableSet tables) => MsSqlOutboxSqlLayer.SchemaCreationSql(tables))
               .MsSqlSchemaContribution((EndpointTableSet tables) => MsSqlPeerRegistrySqlLayer.SchemaCreationSql(tables))
               .MsSqlSchemaContribution(MsSqlEndpointCatalogSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IEndpointCatalogSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlEndpointCatalogSqlLayer(endpointSqlConnection, schemaManager)),
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new MsSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new MsSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, EndpointTableSet tables) => new MsSqlPeerRegistrySqlLayer(endpointSqlConnection, schemaManager, tables)));
}

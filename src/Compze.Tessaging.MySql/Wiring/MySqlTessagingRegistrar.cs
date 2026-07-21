using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MySql;
using Compze.Sql.MySql.Wiring;
using Compze.Tessaging._internal.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;
using Compze.Sql.MySql.Wiring._internal;
using Compze.Tessaging.MySql._private;
using Compze.Sql.MySql._internal;

namespace Compze.Tessaging.MySql.Wiring;

public static class MySqlTessagingRegistrar
{
   public static IComponentRegistrar MySqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlTypeIdInterner()
               .MySqlSchemaContribution((EndpointTableSet tables) => MySqlInboxSqlLayer.SchemaCreationSql(tables))
               .MySqlSchemaContribution((EndpointTableSet tables) => MySqlOutboxSqlLayer.SchemaCreationSql(tables))
               .MySqlSchemaContribution((EndpointTableSet tables) => MySqlPeerRegistrySqlLayer.SchemaCreationSql(tables))
               .MySqlSchemaContribution(MySqlEndpointCatalogSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IEndpointCatalogSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager) => new MySqlEndpointCatalogSqlLayer(endpointSqlConnection, schemaManager)),
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new MySqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new MySqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, EndpointTableSet tables) => new MySqlPeerRegistrySqlLayer(endpointSqlConnection, schemaManager, tables)));
}

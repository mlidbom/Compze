using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;

namespace Compze.Tessaging.MySql.Wiring;

public static class MySqlTessagingRegistrar
{
   extension(ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: MySQL, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the type-id interner Tessaging's sql layers share, and Tessaging's MySQL sql layers.</summary>
      public ExactlyOnceEndpointBuilder MySqlDomainDatabase(string connectionStringName) =>
         @this.ConfigurePersistence(registrar => registrar.MySqlDomainDatabase(connectionStringName)
                                                    .MySqlTessagingSqlLayer());
   }

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

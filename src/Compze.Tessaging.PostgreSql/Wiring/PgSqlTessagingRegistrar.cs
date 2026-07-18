using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;

namespace Compze.Tessaging.PostgreSql.Wiring;

public static class PgSqlTessagingRegistrar
{
   extension(ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: PostgreSQL, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the type-id interner Tessaging's sql layers share, and Tessaging's PostgreSQL sql layers.</summary>
      public ExactlyOnceEndpointBuilder PgSqlDomainDatabase(string connectionStringName) =>
         @this.DomainDatabase(registrar => registrar.PgSqlDomainDatabase(connectionStringName)
                                                    .PgSqlTessagingSqlLayer());
   }

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

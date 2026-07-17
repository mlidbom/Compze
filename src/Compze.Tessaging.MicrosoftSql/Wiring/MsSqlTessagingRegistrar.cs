using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;

namespace Compze.Tessaging.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   extension(ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: SQL Server, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the type-id interner Tessaging's sql layers share, and Tessaging's SQL Server sql layers.</summary>
      public void MsSqlDomainDatabase(string connectionStringName) =>
         @this.DomainDatabase(registrar => registrar.MsSqlDomainDatabase(connectionStringName)
                                                    .MsSqlTessagingSqlLayer());
   }

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

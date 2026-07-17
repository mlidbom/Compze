using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Private;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;

namespace Compze.Tessaging.MySql.Wiring;

public static class MySqlTessagingRegistrar
{
   extension(Compze.Tessaging.Endpoints.ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: MySQL, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the type-id interner Tessaging's sql layers share, and Tessaging's MySQL sql layers.</summary>
      public void MySqlDomainDatabase(string connectionStringName) =>
         @this.DomainDatabase(registrar => registrar.MySqlDomainDatabase(connectionStringName)
                                                    .MySqlTessagingSqlLayer());
   }

   public static IComponentRegistrar MySqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlTypeIdInterner()
               .MySqlSchemaContribution(MySqlInboxSqlLayer.SchemaCreationSql)
               .MySqlSchemaContribution(MySqlOutboxSqlLayer.SchemaCreationSql)
               .MySqlSchemaContribution(MySqlPeerRegistrySqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MySqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection, MySqlSqlLayerSchemaManager schemaManager) => new MySqlPeerRegistrySqlLayer(endpointSqlConnection, schemaManager)));
}

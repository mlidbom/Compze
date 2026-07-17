using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql;
using Compze.Internals.Sql.MicrosoftSql.Private;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MicrosoftSql.Wiring;

namespace Compze.Tessaging.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   extension(Compze.Tessaging.Endpoints.ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's database: SQL Server, reached through <paramref name="connectionStringName"/> — filling<br/>
      /// the exactly-once endpoint's one database parameter with the whole engine pairing: the connection pool, the type-id<br/>
      /// interner Tessaging's sql layers share, and Tessaging's SQL Server sql layers.</summary>
      public void MsSqlEndpointDatabase(string connectionStringName) =>
         @this.Database(registrar => registrar.MsSqlEndpointDatabase(connectionStringName)
                                              .MsSqlTessagingSqlLayer());
   }

   public static IComponentRegistrar MsSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MsSqlTypeIdInterner()
               .MsSqlSchemaContribution(MsSqlInboxSqlLayer.SchemaCreationSql)
               .MsSqlSchemaContribution(MsSqlOutboxSqlLayer.SchemaCreationSql)
               .MsSqlSchemaContribution(MsSqlPeerRegistrySqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new MsSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection, MsSqlSqlLayerSchemaManager schemaManager) => new MsSqlPeerRegistrySqlLayer(endpointSqlConnection, schemaManager)));
}

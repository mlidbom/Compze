using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

namespace Compze.Tessaging.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   extension(ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the domain database this endpoint joins: sqlite, reached through <paramref name="connectionStringName"/> —<br/>
      /// filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing: the connection pool,<br/>
      /// the sqlite type-id interner Tessaging's sql layers share (derived from the declaration), and Tessaging's sqlite sql layers.</summary>
      public void SqliteDomainDatabase(string connectionStringName) =>
         @this.DomainDatabase(registrar => registrar.SqliteDomainDatabase(connectionStringName)
                                                    .SqliteTypeIdInterner(new SqliteDomainDatabase(connectionStringName))
                                                    .SqliteTessagingSqlLayer());
   }

   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution((EndpointTableSet tables) => SqliteInboxSqlLayer.SchemaCreationSql(tables))
               .SqliteSchemaContribution((EndpointTableSet tables) => SqliteOutboxSqlLayer.SchemaCreationSql(tables))
               .SqliteSchemaContribution((EndpointTableSet tables) => SqlitePeerRegistrySqlLayer.SchemaCreationSql(tables))
               .Register(
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner, tables)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, EndpointTableSet tables) => new SqlitePeerRegistrySqlLayer(endpointSqlConnection, schemaManager, tables)));
}

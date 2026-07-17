using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.Sqlite.Wiring;

namespace Compze.Tessaging.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   extension(Compze.Tessaging.Endpoints.ExactlyOnceEndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's database: sqlite, reached through <paramref name="connectionStringName"/> — filling the<br/>
      /// exactly-once endpoint's one database parameter with the whole engine pairing: the connection pool, the sqlite type-id<br/>
      /// interner Tessaging's sql layers share (derived from the declaration), and Tessaging's sqlite sql layers.</summary>
      public void SqliteEndpointDatabase(string connectionStringName) =>
         @this.Database(registrar => registrar.SqliteEndpointDatabase(connectionStringName)
                                              .SqliteTypeIdInterner(new SqliteEndpointDatabase(connectionStringName))
                                              .SqliteTessagingSqlLayer());
   }

   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(SqliteInboxSqlLayer.SchemaCreationSql)
               .SqliteSchemaContribution(SqliteOutboxSqlLayer.SchemaCreationSql)
               .SqliteSchemaContribution(SqlitePeerRegistrySqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<ITessagingSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<ITessagingSqlLayer.IPeerRegistrySqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager) => new SqlitePeerRegistrySqlLayer(endpointSqlConnection, schemaManager)));
}

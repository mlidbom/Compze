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
   extension(EndpointFoundation<SqliteEndpointDatabase> @this)
   {
      ///<summary>Adds exactly-once Tessaging to an endpoint whose database is sqlite: registers Tessaging's inbox/outbox sql layers<br/>
      /// (<see cref="SqliteTessagingSqlLayer"/>) in the endpoint's database — plus the sqlite type-id interner the sql layers<br/>
      /// share, derived from the foundation's declaration — runs <paramref name="compose"/> to fill the feature's slots (e.g. the<br/>
      /// serializer), and adds the feature. The compiler routes this pairing through the foundation's type — Tessaging-on-sqlite<br/>
      /// exists only for an endpoint whose foundation declares a sqlite database.</summary>
      public ExactlyOnceTessagingEndpointFeature AddExactlyOnceTessaging(Action<ExactlyOnceTessagingComposition> compose)
      {
         @this.Builder.Registrar.SqliteTypeIdInterner(@this.Database)
                                .SqliteTessagingSqlLayer();
         compose(new ExactlyOnceTessagingComposition(@this.Builder.Registrar));
         return @this.Builder.AddExactlyOnceTessaging();
      }
   }

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

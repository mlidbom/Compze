using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;

namespace Compze.Tessaging.Sqlite.Wiring;

public static class SqliteTessagingRegistrar
{
   extension(EndpointFoundation<SqliteEndpointDatabase> @this)
   {
      ///<summary>Adds distributed Tessaging to an endpoint whose database is sqlite: registers Tessaging's inbox/outbox sql layers<br/>
      /// (<see cref="SqliteTessagingSqlLayer"/>) in the endpoint's database, runs <paramref name="compose"/> to fill the feature's<br/>
      /// slots (e.g. the serializer), and adds the feature. The compiler routes this pairing through the foundation's type —<br/>
      /// Tessaging-on-sqlite exists only for an endpoint whose foundation declares a sqlite database.</summary>
      public DistributedTessagingEndpointFeature AddDistributedTessaging(Action<DistributedTessagingComposition> compose)
      {
         @this.Builder.Registrar.SqliteTessagingSqlLayer();
         compose(new DistributedTessagingComposition(@this.Builder.Registrar));
         return @this.Builder.AddDistributedTessaging();
      }
   }

   public static IComponentRegistrar SqliteTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(SqliteInboxSqlLayer.SchemaCreationSql)
               .SqliteSchemaContribution(SqliteOutboxSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}

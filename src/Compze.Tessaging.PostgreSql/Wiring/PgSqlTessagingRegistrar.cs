using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;

namespace Compze.Tessaging.PostgreSql.Wiring;

public static class PgSqlTessagingRegistrar
{
   extension(EndpointFoundation<PgSqlEndpointDatabase> @this)
   {
      ///<summary>Adds exactly-once Tessaging to an endpoint whose database is PostgreSQL: registers Tessaging's inbox/outbox sql layers<br/>
      /// (<see cref="PgSqlTessagingSqlLayer"/>) in the endpoint's database, runs <paramref name="compose"/> to fill the feature's<br/>
      /// slots (e.g. the serializer), and adds the feature. The compiler routes this pairing through the foundation's type —<br/>
      /// Tessaging-on-PostgreSQL exists only for an endpoint whose foundation declares a PostgreSQL database.</summary>
      public ExactlyOnceTessagingEndpointFeature AddExactlyOnceTessaging(Action<ExactlyOnceTessagingComposition> compose)
      {
         @this.Builder.Registrar.PgSqlTessagingSqlLayer();
         compose(new ExactlyOnceTessagingComposition(@this.Builder.Registrar));
         return @this.Builder.AddExactlyOnceTessaging();
      }
   }

   public static IComponentRegistrar PgSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlTypeIdInterner()
               .PgSqlSchemaContribution(PgSqlInboxSqlLayer.SchemaCreationSql)
               .PgSqlSchemaContribution(PgSqlOutboxSqlLayer.SchemaCreationSql)
               .Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlOutboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new PgSqlInboxSqlLayer(endpointSqlConnection, schemaManager, typeIdInterner)));
}

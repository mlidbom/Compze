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
   extension(EndpointFoundation<MsSqlEndpointDatabase> @this)
   {
      ///<summary>Adds exactly-once Tessaging to an endpoint whose database is SQL Server: registers Tessaging's inbox/outbox sql layers<br/>
      /// (<see cref="MsSqlTessagingSqlLayer"/>) in the endpoint's database, runs <paramref name="compose"/> to fill the feature's<br/>
      /// slots (e.g. the serializer), and adds the feature. The compiler routes this pairing through the foundation's type —<br/>
      /// Tessaging-on-SQL-Server exists only for an endpoint whose foundation declares a SQL Server database.</summary>
      public ExactlyOnceTessagingEndpointFeature AddExactlyOnceTessaging(Action<ExactlyOnceTessagingComposition> compose)
      {
         @this.Builder.Registrar.MsSqlTessagingSqlLayer();
         compose(new ExactlyOnceTessagingComposition(@this.Builder.Registrar));
         return @this.Builder.AddExactlyOnceTessaging();
      }
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

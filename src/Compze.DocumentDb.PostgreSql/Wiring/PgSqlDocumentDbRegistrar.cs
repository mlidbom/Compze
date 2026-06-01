using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Private;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.DocumentDb.PostgreSql.PgSqlDocumentDbSqlLayer;

namespace Compze.DocumentDb.PostgreSql.Wiring;

public static class PgSqlDocumentDbRegistrar
{
   public static string SchemaCreationSql => Layer.SchemaCreationSql;

   public static IComponentRegistrar PgSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

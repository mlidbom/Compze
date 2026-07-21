using Compze.DocumentDb._internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.PostgreSql._internal;
using Compze.Sql.PostgreSql.Wiring;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Layer = Compze.DocumentDb.PostgreSql._private.PgSqlDocumentDbSqlLayer;
using Compze.Sql.PostgreSql.Wiring._internal;

namespace Compze.DocumentDb.PostgreSql.Wiring;

public static class PgSqlDocumentDbRegistrar
{
   public static IComponentRegistrar PgSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.PgSqlTypeIdInterner()
               .PgSqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider, PgSqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

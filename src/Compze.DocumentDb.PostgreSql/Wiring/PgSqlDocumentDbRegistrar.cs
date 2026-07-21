using Compze.DocumentDb._internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.PostgreSql;
using Compze.Internals.Sql.PostgreSql.Wiring;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.PostgreSql.Wiring;
using Layer = Compze.DocumentDb.PostgreSql._private.PgSqlDocumentDbSqlLayer;
using Compze.Internals.Sql.PostgreSql.Wiring._internal;
using Compze.Internals.Sql.PostgreSql._internal;

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

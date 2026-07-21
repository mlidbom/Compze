using Compze.DocumentDb._internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MySql;
using Compze.Internals.Sql.MySql.Wiring;
using Compze.TypeIdentifiers.Interning;
using Compze.TypeIdentifiers.Interning.MySql.Wiring;
using Layer = Compze.DocumentDb.MySql._private.MySqlDocumentDbSqlLayer;
using Compze.Internals.Sql.MySql.Wiring._internal;
using Compze.Internals.Sql.MySql._internal;

namespace Compze.DocumentDb.MySql.Wiring;

public static class MySqlDocumentDbRegistrar
{
   public static IComponentRegistrar MySqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.MySqlTypeIdInterner()
               .MySqlSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

using Compze.DocumentDb._internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Sql.Sqlite._internal;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.DocumentDb.Sqlite._private.SqliteDocumentDbSqlLayer;
using Compze.Sql.Sqlite.Wiring._internal;

namespace Compze.DocumentDb.Sqlite.Wiring;

public static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

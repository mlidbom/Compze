using Compze.DocumentDb._internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.DocumentDb.Sqlite._private.SqliteDocumentDbSqlLayer;
using Compze.Internals.Sql.Sqlite.Wiring._internal;
using Compze.Internals.Sql.Sqlite._internal;

namespace Compze.DocumentDb.Sqlite.Wiring;

public static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.DocumentDb.Sqlite.SqliteDocumentDbSqlLayer;

namespace Compze.DocumentDb.Sqlite.Wiring;

public static class SqliteDocumentDbRegistrar
{
   public static string SchemaCreationSql => Layer.SchemaCreationSql;

   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

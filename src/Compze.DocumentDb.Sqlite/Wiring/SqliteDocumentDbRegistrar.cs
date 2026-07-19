using Compze.DocumentDb.Internal.SqlLayer;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.Sql.Sqlite;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Wiring;
using Compze.TypeIdentifiers.Interning;
using Layer = Compze.DocumentDb.Sqlite.SqliteDocumentDbSqlLayer;

namespace Compze.DocumentDb.Sqlite.Wiring;

public static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.SqliteSchemaContribution(Layer.SchemaCreationSql)
               .Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new Layer(connectionProvider, schemaManager, typeIdInterner)));
}

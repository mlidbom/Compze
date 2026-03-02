using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Sql.Sqlite.Private;
using Compze.Sql.Sqlite.Private.DocumentDb;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager) => new SqliteDocumentDbSqlLayer(connectionProvider, schemaManager)))
               .SqliteSqlLayerSchemaManager();
}

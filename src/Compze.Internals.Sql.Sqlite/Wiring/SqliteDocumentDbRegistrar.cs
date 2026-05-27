using Compze.DocumentDb.Internal.SqlLayer;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.Sql.Sqlite.Private;
using Compze.Internals.Sql.Sqlite.Private.DocumentDb;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider, SqliteSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner) => new SqliteDocumentDbSqlLayer(connectionProvider, schemaManager, typeIdInterner)))
               .SqliteTypeIdInterner();
}

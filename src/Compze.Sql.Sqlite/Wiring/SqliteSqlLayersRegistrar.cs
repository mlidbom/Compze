using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteSqlLayersRegistrar
{
   public static IComponentRegistrar SqliteDSqliteSqlLayers(this IComponentRegistrar registrar) =>
      registrar.SqliteSqlLayerSchemaManager()
               .SqliteDocumentDbSqlLayer()
               .SqliteTessagingSqlLayer()
               .SqliteTeventStoreSqlLayer();
}

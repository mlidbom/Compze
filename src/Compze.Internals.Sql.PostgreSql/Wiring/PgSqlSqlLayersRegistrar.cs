using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

public static class PgSqlSqlLayersRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayers(this IComponentRegistrar registrar) =>
      registrar.PgSqlSqlLayerSchemaManager()
               .PgSqlDocumentDbSqlLayer()
               .PgSqlTessagingSqlLayer()
               .PgSqlTeventStoreSqlLayer();
}

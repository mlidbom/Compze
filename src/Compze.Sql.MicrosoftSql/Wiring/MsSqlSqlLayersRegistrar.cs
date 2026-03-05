using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

public static class MsSqlSqlLayersRegistrar
{
   public static IComponentRegistrar MsSqlSqlLayers(this IComponentRegistrar registrar) =>
      registrar.MsSqlSqlLayerSchemaManager()
               .MsSqlDocumentDbSqlLayer()
               .MsSqlTessagingSqlLayer()
               .MsSqlTeventStoreSqlLayer();
}

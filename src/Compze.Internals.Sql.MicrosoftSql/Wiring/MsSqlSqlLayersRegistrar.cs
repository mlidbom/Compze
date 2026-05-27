using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

public static class MsSqlSqlLayersRegistrar
{
   public static IComponentRegistrar MsSqlSqlLayers(this IComponentRegistrar registrar) =>
      registrar.MsSqlSqlLayerSchemaManager()
               .MsSqlTypeIdInterner()
               .MsSqlDocumentDbSqlLayer()
               .MsSqlTessagingSqlLayer()
               .MsSqlTeventStoreSqlLayer();
}

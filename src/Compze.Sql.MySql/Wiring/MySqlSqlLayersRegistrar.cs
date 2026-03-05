using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

public static class MySqlSqlLayersRegistrar
{
   public static IComponentRegistrar MySqlSqlLayers(this IComponentRegistrar registrar) =>
      registrar.MySqlSqlLayerSchemaManager()
               .MySqlDocumentDbSqlLayer().
                MySqlTessagingSqlLayer().
                MySqlTeventStoreSqlLayer();
}

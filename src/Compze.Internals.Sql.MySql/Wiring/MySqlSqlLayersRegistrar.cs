using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

public static class MySqlSqlLayersRegistrar
{
   public static IComponentRegistrar MySqlSqlLayers(this IComponentRegistrar registrar) =>
      registrar.MySqlSqlLayerSchemaManager()
               .MySqlTypeIdInterner()
               .MySqlDocumentDbSqlLayer().
                MySqlTessagingSqlLayer().
                MySqlTeventStoreSqlLayer();
}

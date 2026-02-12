using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteDbPoolSqlLayerRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

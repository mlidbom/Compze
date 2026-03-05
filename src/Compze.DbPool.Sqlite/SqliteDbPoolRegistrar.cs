using Compze.DependencyInjection.Abstractions;

namespace Compze.DbPool.Sqlite;

public static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.Testing.DbPool.Sqlite;

public static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

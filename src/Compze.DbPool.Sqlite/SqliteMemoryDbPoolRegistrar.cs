using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.DbPool.Sqlite;

public static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

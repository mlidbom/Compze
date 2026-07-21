using Compze.DependencyInjection.Abstractions;

namespace Compze.DbPool.Sqlite;

public static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite._private.SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

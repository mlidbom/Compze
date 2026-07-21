using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.Sqlite._private;

namespace Compze.DbPool.Sqlite;

public static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite._private.SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

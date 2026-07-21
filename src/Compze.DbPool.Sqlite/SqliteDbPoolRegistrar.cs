using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.Sqlite._private;

namespace Compze.DbPool.Sqlite;

public static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite._private.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

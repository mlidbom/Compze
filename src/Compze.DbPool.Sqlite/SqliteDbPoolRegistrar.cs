using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.Sqlite.Private;

namespace Compze.DbPool.Sqlite;

public static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.Private.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

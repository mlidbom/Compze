using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DbPool.Sqlite;

public static class SqliteDbPoolRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

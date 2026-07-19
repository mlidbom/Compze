using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DbPool.Sqlite;

public static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Sqlite.SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

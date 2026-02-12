using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

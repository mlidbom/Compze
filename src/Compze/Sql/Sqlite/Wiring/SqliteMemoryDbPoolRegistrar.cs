using Compze.Sql.Sqlite.Private.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

static class SqliteMemoryDbPoolRegistrar
{
   public static IComponentRegistrar SqliteMemoryDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      SqliteMemoryDbPoolSqlLayer.RegisterWith(registrar);
}

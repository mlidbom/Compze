using Compze.Sql.Sqlite.Private.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class SqliteDbPoolSqlLayerRegistrar
{
   public static IComponentRegistrar SqliteDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      SqliteDbPoolSqlLayer.RegisterWith(registrar);
}

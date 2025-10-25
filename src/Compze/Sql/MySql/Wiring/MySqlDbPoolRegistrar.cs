using Compze.Sql.MySql.Private.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      MySqlDbPoolSqlLayer.RegisterWith(registrar);
}

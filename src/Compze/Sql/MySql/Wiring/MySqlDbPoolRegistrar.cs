using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.MySqlDbPoolSqlLayer.RegisterWith(registrar);
}

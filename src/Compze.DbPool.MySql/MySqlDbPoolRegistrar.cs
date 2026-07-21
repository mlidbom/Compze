using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.MySql._private;

namespace Compze.DbPool.MySql;

public static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MySql._private.MySqlDbPoolSqlLayer.RegisterWith(registrar);
}

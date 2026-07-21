using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.MySql.Private;

namespace Compze.DbPool.MySql;

public static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MySql.Private.MySqlDbPoolSqlLayer.RegisterWith(registrar);
}

using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DbPool.MySql;

public static class MySqlDbPoolRegistrar
{
   public static IComponentRegistrar MySqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MySql.MySqlDbPoolSqlLayer.RegisterWith(registrar);
}

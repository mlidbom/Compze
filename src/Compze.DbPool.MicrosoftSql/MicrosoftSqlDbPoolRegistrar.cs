using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DbPool.MicrosoftSql;

public static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MicrosoftSql.MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

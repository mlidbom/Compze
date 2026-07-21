using Compze.DependencyInjection.Abstractions;

namespace Compze.DbPool.MicrosoftSql;

public static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MicrosoftSql._private.MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.Testing.DbPool.MicrosoftSql;

public static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MicrosoftSql.MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

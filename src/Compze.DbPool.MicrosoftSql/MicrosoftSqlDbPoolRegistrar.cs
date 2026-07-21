using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.MicrosoftSql.Private;

namespace Compze.DbPool.MicrosoftSql;

public static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      MicrosoftSql.Private.MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

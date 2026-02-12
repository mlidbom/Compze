using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

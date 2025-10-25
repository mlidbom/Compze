using Compze.Sql.MicrosoftSql.Private.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

static class MicrosoftSqlDbPoolRegistrar
{
   public static IComponentRegistrar MsSqlDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      MsSqlDbPoolSqlLayer.RegisterWith(registrar);
}

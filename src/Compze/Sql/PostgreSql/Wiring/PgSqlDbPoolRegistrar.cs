using Compze.Sql.PostgreSql.Private.DbPool;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayerIfNotAlreadyRegistered(this IComponentRegistrar registrar) =>
      PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

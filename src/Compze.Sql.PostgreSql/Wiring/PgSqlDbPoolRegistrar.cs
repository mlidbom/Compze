using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

public static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      Private.DbPool.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

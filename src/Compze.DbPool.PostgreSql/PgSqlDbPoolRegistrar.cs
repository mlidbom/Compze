using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.PostgreSql.Private;

namespace Compze.DbPool.PostgreSql;

public static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      PostgreSql.Private.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

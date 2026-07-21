using Compze.DependencyInjection.Abstractions;
using Compze.DbPool.PostgreSql._private;

namespace Compze.DbPool.PostgreSql;

public static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      PostgreSql._private.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

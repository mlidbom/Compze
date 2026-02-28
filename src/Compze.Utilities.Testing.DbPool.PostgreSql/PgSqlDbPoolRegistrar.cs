using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.Testing.DbPool.PostgreSql;

public static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      PostgreSql.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DbPool.PostgreSql;

public static class PgSqlDbPoolRegistrar
{
   public static IComponentRegistrar PgSqlDbPoolSqlLayer(this IComponentRegistrar registrar) =>
      PostgreSql.PgSqlDbPoolSqlLayer.RegisterWith(registrar);
}

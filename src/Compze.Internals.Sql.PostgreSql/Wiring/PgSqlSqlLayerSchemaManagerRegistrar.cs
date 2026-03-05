using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.PostgreSql.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.PgSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

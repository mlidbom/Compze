using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.PgSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

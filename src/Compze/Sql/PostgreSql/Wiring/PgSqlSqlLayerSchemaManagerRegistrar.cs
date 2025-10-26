using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   internal static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.PgSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

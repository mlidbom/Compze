using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

internal static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.PgSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

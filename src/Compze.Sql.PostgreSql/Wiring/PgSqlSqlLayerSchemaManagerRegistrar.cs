using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Wiring;

public static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar PgSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.PgSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

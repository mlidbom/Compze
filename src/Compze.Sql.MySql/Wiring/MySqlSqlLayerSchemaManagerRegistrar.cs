using Compze.DependencyInjection.Abstractions;

namespace Compze.Sql.MySql.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar MySqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.MySqlSqlLayerSchemaManager.RegisterWith(registrar);
}

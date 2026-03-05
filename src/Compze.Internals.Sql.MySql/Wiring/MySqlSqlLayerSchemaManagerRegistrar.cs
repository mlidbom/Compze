using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MySql.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar MySqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.MySqlSqlLayerSchemaManager.RegisterWith(registrar);
}

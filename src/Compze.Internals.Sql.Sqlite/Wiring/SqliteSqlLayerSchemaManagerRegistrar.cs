using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.Sqlite.Wiring;

static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar SqliteSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.SqliteSqlLayerSchemaManager.RegisterWith(registrar);
}

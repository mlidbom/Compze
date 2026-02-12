using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.Wiring;

public static class PgSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar SqliteSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.SqliteSqlLayerSchemaManager.RegisterWith(registrar);
}

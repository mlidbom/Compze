using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlSqlLayerSchemaManagerRegistrar
{
   public static IComponentRegistrar MsSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.MsSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

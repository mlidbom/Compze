using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

static class MsSqlSqlLayerSchemaManagerRegistrar
{
   internal static IComponentRegistrar MsSqlSqlLayerSchemaManager(this IComponentRegistrar registrar) =>
      Private.MsSqlSqlLayerSchemaManager.RegisterWith(registrar);
}

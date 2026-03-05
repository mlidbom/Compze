using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Sql.MicrosoftSql.Wiring;

static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      Private.DocumentDb.MsSqlDocumentDbSqlLayer.RegisterWith(registrar)
             .MsSqlSqlLayerSchemaManager();
}

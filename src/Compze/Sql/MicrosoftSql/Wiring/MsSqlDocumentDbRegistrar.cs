using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

public static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      Private.DocumentDb.MsSqlDocumentDbSqlLayer.RegisterWith(registrar)
             .MsSqlSqlLayerSchemaManager();
}

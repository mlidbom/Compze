using Compze.DependencyInjection.Abstractions;
using Layer = Compze.DocumentDb.MicrosoftSql.MsSqlDocumentDbSqlLayer;

namespace Compze.DocumentDb.MicrosoftSql.Wiring;

public static class MsSqlDocumentDbRegistrar
{
   public static string SchemaCreationSql => Layer.SchemaCreationSql;

   public static IComponentRegistrar MsSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      Layer.RegisterWith(registrar);
}

using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Sql.MicrosoftSql.Wiring;
using Layer = Compze.DocumentDb.MicrosoftSql.MsSqlDocumentDbSqlLayer;

namespace Compze.DocumentDb.MicrosoftSql.Wiring;

public static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      Layer.RegisterWith(registrar.MsSqlSchemaContribution(Layer.SchemaCreationSql));
}

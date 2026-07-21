using Compze.DependencyInjection.Abstractions;
using Compze.Sql.MicrosoftSql.Wiring;
using Layer = Compze.DocumentDb.MicrosoftSql._private.MsSqlDocumentDbSqlLayer;
using Compze.Sql.MicrosoftSql.Wiring._internal;

namespace Compze.DocumentDb.MicrosoftSql.Wiring;

public static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDbSqlLayer(this IComponentRegistrar registrar) =>
      Layer.RegisterWith(registrar.MsSqlSchemaContribution(Layer.SchemaCreationSql));
}

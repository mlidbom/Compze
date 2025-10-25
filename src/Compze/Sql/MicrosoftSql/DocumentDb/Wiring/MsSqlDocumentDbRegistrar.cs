using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.DocumentDb.Wiring;

public static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbSqlLayer(connectionProvider)));
}

using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.MicrosoftSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.MicrosoftSql;

public static class MsSqlDocumentDbRegistrar
{
   public static IComponentRegistrar MsSqlDocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbSqlLayer(connectionProvider)));
}

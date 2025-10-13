using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.MicrosoftSql.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.MicrosoftSql;

public static class MsSqlDocumentDbRegistrar
{
   public static IDependencyRegistrar MsSqlDocumentDb(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbSqlLayer(connectionProvider)));
}

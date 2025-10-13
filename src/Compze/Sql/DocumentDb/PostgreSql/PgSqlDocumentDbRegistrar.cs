using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.PostgreSql.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.PostgreSql;

public static class PgSqlDocumentDbRegistrar
{
   public static IDependencyRegistrar PgSqlDocumentDb(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider) => new PgSqlDocumentDbSqlLayer(connectionProvider)));
}

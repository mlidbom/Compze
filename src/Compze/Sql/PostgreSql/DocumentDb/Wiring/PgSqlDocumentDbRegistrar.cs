using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.PostgreSql;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.PostgreSql.Wiring;

public static class PgSqlDocumentDbRegistrar
{
   public static IComponentRegistrar PgSqlDocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider) => new PgSqlDocumentDbSqlLayer(connectionProvider)));
}

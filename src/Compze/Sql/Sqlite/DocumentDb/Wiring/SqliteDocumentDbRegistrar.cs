using Compze.Core.DocumentDb.Internal.SqlLayer;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.Sqlite.DocumentDb.Wiring;

public static class SqliteDocumentDbRegistrar
{
   public static IComponentRegistrar SqliteDocumentDb(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider) => new SqliteDocumentDbSqlLayer(connectionProvider)));
}

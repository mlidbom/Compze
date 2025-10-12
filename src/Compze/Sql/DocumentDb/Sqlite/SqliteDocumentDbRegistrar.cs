using Compze.Abstractions.Internal.Sql.DocumentDb;
using Compze.Sql.Sqlite.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.Sqlite;

public static class SqliteDocumentDbRegistrar
{
   public static IDependencyRegistrar SqliteDocumentDb(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool connectionProvider) => new SqliteDocumentDbSqlLayer(connectionProvider)));
}

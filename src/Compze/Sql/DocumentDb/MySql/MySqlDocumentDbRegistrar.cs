using Compze.Sql.DocumentDb.Abstractions.Internal;
using Compze.Sql.MySql.Infrastructure.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.DocumentDb.MySql;

public static class MySqlDocumentDbRegistrar
{
   public static IDependencyRegistrar MySqlDocumentDb(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IDocumentDbSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbSqlLayer(connectionProvider)));
}

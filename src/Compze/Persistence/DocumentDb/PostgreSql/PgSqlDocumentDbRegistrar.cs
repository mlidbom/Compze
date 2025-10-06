using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.DependencyInjection;
using Compze.Persistence.PostgreSql.Infrastructure;

namespace Compze.DocumentDb.PostgreSql;

public static class PgSqlDocumentDbRegistrar
{
   public static void RegisterPgSqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider) => new PgSqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

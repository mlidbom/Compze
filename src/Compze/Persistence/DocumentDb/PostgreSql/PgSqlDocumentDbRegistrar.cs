using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Persistence.DocumentDb.PostgreSql;

public static class PgSqlDocumentDbRegistrar
{
   public static void RegisterPgSqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool connectionProvider) => new PgSqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.DependencyInjection;
using Compze.Persistence.MicrosoftSql.Infrastructure;

namespace Compze.DocumentDb.MicrosoftSql;

public static class MsSqlDocumentDbRegistrar
{
   public static void RegisterMsSqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

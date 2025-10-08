using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Persistence.DocumentDb.MicrosoftSql;

public static class MsSqlDocumentDbRegistrar
{
   public static void RegisterMsSqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool connectionProvider) => new MsSqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

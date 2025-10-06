using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Utilities.DependencyInjection;

namespace Compze.Persistence.DocumentDb.MySql;

public static class MySqlDocumentDbRegistrar
{
   public static void RegisterMySqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

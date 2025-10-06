using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.DependencyInjection;
using Compze.Persistence.MySql.Infrastructure;
using Compze.Persistence.MySql.Infrastructure.SystemExtensions;

namespace Compze.DocumentDb.MySql;

public static class MySqlDocumentDbRegistrar
{
   public static void RegisterMySqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

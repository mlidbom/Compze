using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Persistence.DocumentDb.MySql;

public static class MySqlDocumentDbRegistrar
{
   public static IDependencyRegistrar MySqlDocumentDb(this IDependencyRegistrar registrar)
   {
      registrar.Container().RegisterMySqlDocumentDb();
      return registrar;
    }

    public static void RegisterMySqlDocumentDb(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool connectionProvider) => new MySqlDocumentDbPersistenceLayer(connectionProvider)));
   }
}

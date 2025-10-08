using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Persistence.MicrosoftSql;

public static class MsSqlTessagingRegistrar
{
   public static IDependencyRegistrar MsSqlTessaging(this IDependencyRegistrar registrar)
   {
      registrar.Container().RegisterMsSqlTessaging();
      return registrar;
   }

   public static void RegisterMsSqlTessaging(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlInboxPersistenceLayer(endpointSqlConnection)));
   }
}

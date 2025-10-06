using Compze.DependencyInjection;
using Compze.Persistence.MicrosoftSql.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.MicrosoftSql;

public static class MsSqlTessagingRegistrar
{
   public static void RegisterMsSqlTessaging(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlInboxPersistenceLayer(endpointSqlConnection)));
   }
}

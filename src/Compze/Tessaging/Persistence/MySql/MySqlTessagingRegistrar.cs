using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Persistence.MySql;

public static class MySqlTessagingRegistrar
{
   public static void RegisterMySqlTessaging(this IDependencyInjectionContainer container)
   {
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlInboxPersistenceLayer(endpointSqlConnection)));
   }
}

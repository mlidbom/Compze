using Compze.DependencyInjection;
using Compze.Persistence.MySql.Infrastructure;
using Compze.Persistence.MySql.Infrastructure.SystemExtensions;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.MySql;

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

using Compze.Persistence.PostgreSql.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Persistence.PostgreSql;

public static class PgSqlTessagingRegistrar
{
   public static IDependencyRegistrar PgSqlTessaging(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlOutboxPersistenceLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlInboxPersistenceLayer(endpointSqlConnection)));
}

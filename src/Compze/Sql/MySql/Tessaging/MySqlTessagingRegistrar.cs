using Compze.Sql.MySql.SystemExtensions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Sql.MySql;

public static class MySqlTessagingRegistrar
{
   public static IComponentRegistrar MySqlTessaging(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlOutboxSqlLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMySqlConnectionPool endpointSqlConnection) => new MySqlInboxSqlLayer(endpointSqlConnection)));
}

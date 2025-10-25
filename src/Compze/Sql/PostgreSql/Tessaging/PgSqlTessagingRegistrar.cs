using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.PostgreSql.Tessaging;

public static class PgSqlTessagingRegistrar
{
   public static IComponentRegistrar PgSqlTessaging(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlOutboxSqlLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IPgSqlConnectionPool endpointSqlConnection) => new PgSqlInboxSqlLayer(endpointSqlConnection)));
}

using Compze.Core.Tessaging.Internal.SqlLayer;
using Compze.Sql.MicrosoftSql.Private.Tessaging;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Sql.MicrosoftSql.Wiring;

public static class MsSqlTessagingRegistrar
{
   public static IComponentRegistrar MsSqlTessagingSqlLayer(this IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlOutboxSqlLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((IMsSqlConnectionPool endpointSqlConnection) => new MsSqlInboxSqlLayer(endpointSqlConnection)));
}

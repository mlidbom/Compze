using Compze.Sql.Sqlite.Infrastructure;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Sql.Sqlite;

public static class SqliteTessagingRegistrar
{
   public static IDependencyRegistrar SqliteTessaging(this IDependencyRegistrar registrar) =>
      registrar.Register(
         Singleton.For<IServiceBusSqlLayer.IOutboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection) => new SqliteOutboxSqlLayer(endpointSqlConnection)),
         Singleton.For<IServiceBusSqlLayer.IInboxSqlLayer>()
                  .CreatedBy((ISqliteConnectionPool endpointSqlConnection) => new SqliteInboxSqlLayer(endpointSqlConnection)));
}
